# optimize server

### step 1: move line or vertex 
### step 2: optimize the line or vertex
### step 3: recalculate for curve, reoptimize for curve
### step 4: pass the state back to front end
import numpy as np
import reoptimize_curve
import scipy


# do not use scientific notion for Unity side
np.set_printoptions(suppress=True)


### lines and points generator
def all_points_positions( points ):
    
    return [node_i_position(i, points) for i in range(len(points))]

def node_i_position(index, points):
    '''
    return (x, y, z)
    '''
    if isinstance(index, int) and points[index][-1] == 'vertex':
        return points[index][:3]
    else:
        r, i0, i1 = points[index][:3]
        return lerp(r, node_i_position(i0, points), node_i_position(i1, points) )


def node_is_tick( pointsData, index):
    '''
    if this node is a tick point
    '''
    return pointsData[index][-1] == 'tick'

def lerp(r, point1, point2):
    point1 = np.asarray( point1 )
    point2 = np.asarray( point2 )
    
    return (1 - r) * point1 + r * point2

def all_lines_positions( linesData, points_positions ):
    result = []

    for line in linesData:
        i0, i1, _ = line
        p0 = points_positions[i0]
        p1 = points_positions[i1]
        result.append( [p0, p1] )

    return result


### constraint related functions
def line_line_parallel2( line1 , line2 ):
    l1start = np.asarray( line1[0] )
    l1end = np.asarray( line1[1] )
    
    l2start = np.asarray( line2[0] )
    l2end = np.asarray( line2[1] )
    
    l1vec = dir( l1end , l1start )
    l2vec = dir( l2end , l2start )
    
    return ( 1 - np.abs( np.dot( l1vec, l2vec )) ) ** 2

def line_line_perpendicular2( line1, line2 ):
    l1start = np.asarray( line1[0] )
    l1end = np.asarray( line1[1] )
    
    l2start = np.asarray( line2[0] )
    l2end = np.asarray( line2[1] )
    
    l1vec = dir( l1end , l1start )
    l2vec = dir( l2end , l2start )

    return ( np.dot( l1vec, l2vec )) ** 2

def point_point_distance2( point1, point2 ):
    point1 = np.asarray( point1 )
    point2 = np.asarray( point2 )
    
    v = ( point1 - point2 )
    return  np.dot( v, v ) 

def point_point_distance( point1, point2):

    return np.sqrt( point_point_distance2(point1, point2))

def length_length_ratio2( length0, length1 ): 
    
    return ( length0 / length1  - 1 ) ** 2

def dir( p0, p1 ):
    """
    normalized direction of p0 p1
    """
    p0 = np.asarray(p0)
    p1 = np.asarray(p1)

    lnorm = point_point_distance(p1,p0)
    assert lnorm > 0
    return (p1 - p0) / lnorm


def project_point_on_line_as_t( point, line ):
    '''
    '''
    p0, p1 = line 

    p0 = np.asarray( p0 )
    p1 = np.asarray( p1 )

    point = np.asarray( point )

    t = np.dot( point - p0, p1 - p0) / np.dot(p1 - p0, p1 - p0)

    return t


# below thresholds chosen from experiments
# parallel_angle : 6 degree (5~10 good)
# perpendiuclar_angle : 5 degree 
# equal_length : 5 percent

thresholds = {
    'line_line_parallel2': (1 - np.cos(6/180*np.pi)) ** 2, # 0.0097 ** 2
    'line_line_perpendicular2': np.cos(85/180*np.pi) ** 2, # 0.087 ** 2
    'line_length_ratio2': 0.05 ** 2, # 0.05 ** 2
}



def find_free_lines_indices( linesData ):
    '''
    find free and half-constrained line vertices
    '''

    free_lines_indices = []

    for i, lineData in enumerate(linesData):
        if lineData[-1] != 'constrained':
            free_lines_indices.append( i )
    
    return free_lines_indices



def calculate_constraints( lines, free_lines ):
    '''
    '''
    constraints = {}
    constraints['equal_length'] = set()
    constraints['parallel'] = set()
    constraints['perpendicular'] = set()

    # i and j : real line index
    # index_i and index_j : index in the free_lines_indices
    for index_i in range(len(free_lines)):
        i = free_lines[index_i]
        line0 = lines[ i ]
        for index_j in range(index_i+1, len(free_lines)):
            j = free_lines[index_j]
            line1 = lines[ j ]


            line0_length = point_point_distance(line0[0], line0[1])
            line1_length = point_point_distance(line1[0], line1[1])



            if length_length_ratio2( line0_length, line1_length ) < thresholds['line_length_ratio2']:
                constraints['equal_length'].add((i,j))

            if line_line_parallel2(line0, line1) < thresholds['line_line_parallel2']:
                constraints['parallel'].add((i,j))

            if line_line_perpendicular2(line0, line1) < thresholds['line_line_perpendicular2']:
                constraints['perpendicular'].add((i,j))


    return constraints




def remove_broken_constraints( constraints, moved_lines ):
    '''
    because the constraints passed in are already filtered constraints,
    I don't need another filter here?
    '''

    c = {}
    c['equal_length'] = set()
    c['parallel'] = set()
    c['perpendicular'] = set()

    for constraint_value in constraints['equal_length']:
        i, j = constraint_value
        p0, p1 = moved_lines[i]
        q0, q1 = moved_lines[j]
        
        length0 = point_point_distance( p0, p1 )
        length1 = point_point_distance( q0, q1 )

        length_energy = length_length_ratio2( length0, length1 )

        if length_energy < thresholds['line_length_ratio2']:
            c['equal_length'].add( constraint_value )

    for constraint_values in constraints['parallel']:
            i, j = constraint_value 
            line0 = moved_lines[i]
            line1 = moved_lines[j]

            parallel_energy = line_line_parallel2(line0, line1)

            if parallel_energy < thresholds['line_line_parallel2']:
                c['parallel'].add( constraint_value )


    for constraint_values in constraints['perpendicular']:
        i, j = constraint_value 
        line0 = moved_lines[i]
        line1 = moved_lines[j]

        perpendicular_energy = line_line_perpendicular2(line0, line1)

        if perpendicular_energy < thresholds['line_line_perpendicular2']:
            c['perpendicular'].add( constraint_value )



    return c

 
def find_updated_edges_indices( lines, moved_lines, free_lines , epsilon = 1e-3):
    '''

    '''

    result = []

    for index in range(len(free_lines)):
        i = free_lines[index]
        if np.allclose(moved_lines[i][0], lines[i][0], atol = epsilon) and np.allclose(moved_lines[i][1], lines[i][1], atol = epsilon):
            continue
        else:
            result.append( i )

    return result


def find_new_constraints(moved_lines, updated_edges_indices, changed_edges_indices, free_lines_indices):
    '''
    moved_lines:
    updated_edges_indices: the updated edges indices
    changed_edges_indices: the actual selected and moved red edges
    free_lines_indices: the free lines set, only the lines both endpoints are vertex
    '''

    c = {}
    c['equal_length'] = set()
    c['parallel'] = set()
    c['perpendicular'] = set()

        
    for i in updated_edges_indices:
        line0 = moved_lines[i]          
        for index_j in range(len(free_lines_indices)):
            j = free_lines_indices[index_j]
            line1 = moved_lines[j]

            if i != j:
                pair = (i, j) if i < j else (j, i)
                if line_line_parallel2(line0, line1) < thresholds['line_line_parallel2']:
                    c['parallel'].add( pair )

                if line_line_perpendicular2(line0, line1) < thresholds['line_line_perpendicular2']:
                    c['perpendicular'].add( pair )

                ## check the length between
                # line0_length = point_point_distance(line0[0], line0[1])
                # line1_length = point_point_distance(line1[0], line1[1])

                # if length_length_ratio2( line0_length, line1_length ) < thresholds['line_length_ratio2']:
                #     c['equal_length'].add( pair ) 

    # check whether there are equal length between the updated lines
    for i in updated_edges_indices:
        line0 = moved_lines[i]
        for j in updated_edges_indices:
            line1 = moved_lines[j]
            if i != j:
                pair = (i, j) if i < j else (j, i)   
                line0_length = point_point_distance(line0[0], line0[1])
                line1_length = point_point_distance(line1[0], line1[1])
                if length_length_ratio2(line0_length, line1_length) < thresholds['line_length_ratio2']:
                    c['equal_length'].add( pair )   

    # the changed_edges_indices are the edges being selected and changed
    # and we compare this with all the free_lines
    for i in changed_edges_indices:
        line0 = moved_lines[i]
        for j in free_lines_indices:
            line1 = moved_lines[j]
            if i != j:
                pair = (i, j) if i < j else (j, i)
                line0_length = point_point_distance(line0[0], line0[1])
                line1_length = point_point_distance(line1[0], line1[1])
                if length_length_ratio2(line0_length, line1_length) < thresholds['line_length_ratio2']:
                    c['equal_length'].add( pair )   


    # print('new_constraints in opt c3', c)


    return c


def add_two_constraints(c_old, c_new):
    c = {}
    c['equal_length'] = set()
    c['parallel'] = set()
    c['perpendicular'] = set()

    for key, val in c_old.items():
        c[key] = c_old[key].union(c_new[key])

    return c

def pack(  points, live_vertex_indices ):
    '''
    pack the live_vertex_indices to an array of numbers
    '''
    X = []

    for live_vertex_index in live_vertex_indices:
        X.extend( points[live_vertex_index] )

    return np.asarray(X)


def unpack( points, live_vertex_indices, X ):
    '''
    put the optimized X back to points
    '''

    i = 0
    
    for live_vertex_index in live_vertex_indices:
        points[live_vertex_index] = X[i*3 : (i+1)*3].tolist()
        i += 1
    
    return points


def optimize_line(points, linesData, live_vertex_indices, constraints, weights):

    # print('points')
    # print(points)
    # print(points[start_point_index])
    X0 = pack( points, live_vertex_indices )   

    # print('X0', X0)
    
    def E( X ):

        e = 0

        new_points = unpack(points, live_vertex_indices, X)
        # print('new_points')
        # print(new_points)
        lines = all_lines_positions(linesData, new_points)
        # print('lines', lines)


        for constraint_type, constraint_values in constraints.items():
            if constraint_type == 'equal_length':
                for constraint_value in constraint_values:
                    i, j = constraint_value
                    p0, p1 = lines[i]
                    q0, q1 = lines[j]
                    # print(i, j)
                    # print(p0, p1)
                    # print(q0, q1)
                    length0 = point_point_distance( p0, p1 )
                    length1 = point_point_distance( q0, q1)
                    length_energy = length_length_ratio2( length0, length1 )
                    e += weights[constraint_type][constraint_value] * length_energy
                    # print('length energy',length_energy)
            if constraint_type == 'parallel': 
                for constraint_value in constraint_values:
                    i, j = constraint_value         
                    line0 = lines[i]
                    line1 = lines[j]
                    # print(i, j)
                    # print(line0)
                    # print(line1)
                    parallel_energy = line_line_parallel2(line0, line1)
                    e += weights[constraint_type][constraint_value] * parallel_energy
                    # print('parallel_energy', parallel_energy)
            if constraint_type == 'perpendicular':
                for constraint_value in constraint_values:
                    i, j = constraint_value
                    line0 = lines[i]
                    line1 = lines[j]
                    # print(i, j)
                    # print(line0)
                    # print(line1)
                    perpendicular_energy = line_line_perpendicular2( line0, line1)
                    e += weights[constraint_type][constraint_value] * perpendicular_energy
                    # print('perpendicular energy', perpendicular_energy)
        return e
       
    result = scipy.optimize.minimize( E,
                                      X0,  
                                      method = 'BFGS', 
                                      tol = 0.000001, 
                                      options = { 'disp': False, 'gtol': 0.000001, 'maxiter': 1000 } 
                                    )

    # print(unpack(points, live_vertex_indices, result.x))
    # return unpack(points, live_vertex_indices, result.x)
    return result

def IRLS( constraints, lines, points, linesData, live_vertex_indices, epsilon = 1e-6 ):
    '''
    lines : moved lines
    points: moved points
    '''
    weights = {}
    weights['parallel'] = {}
    weights['perpendicular'] = {}
    weights['equal_length'] = {}


    for key,val in constraints.items():
        for pair in val:
            weights[key][pair] = 1

    x_previous_iteration = pack(  points, live_vertex_indices )


    while True:

        points = unpack(points, live_vertex_indices, x_previous_iteration)
        lines = all_lines_positions( linesData,  points)

        weights_sum = 0

        for key,val in weights.items():
            if key == 'parallel':
                for pair in val:
                    i, j = pair
                    func_val = line_line_parallel2( lines[i], lines[j] )
                    weights[key][pair] = 1 / ( epsilon + func_val )
                    weights_sum += 1 / ( epsilon +  func_val ) 
            elif key == 'perpendicular':
                for pair in val:
                    i, j = pair
                    func_val = line_line_perpendicular2( lines[i], lines[j] ) 
                    weights[key][pair] = 1 / ( epsilon +  func_val )
                    weights_sum += 1 / ( epsilon +  func_val )
            elif key == 'equal_length':
                for pair in val:
                    i, j = pair
                    length1 = point_point_distance(lines[i][0], lines[i][1])
                    length2 = point_point_distance( lines[j][0], lines[j][1]) 
                    func_val = length_length_ratio2(length1 , length2 )
                    weights[key][pair] = 1 / ( epsilon +  func_val )
                    weights_sum += 1 / ( epsilon + func_val)

    
        for key,val in weights.items():
            for pair in val:
                weights[key][pair] /= weights_sum

        # print('weights', weights)

        result = optimize_line(points, linesData, live_vertex_indices, constraints, weights)


        if np.abs(result.x - x_previous_iteration).sum() < epsilon:   
            break
        else:
            x_previous_iteration = result.x


    # print('optimized points', unpack(points, live_vertex_indices, result.x))
    return unpack(points, live_vertex_indices, result.x)


def move_line( input_data, state ):
    '''
    ## the passed line has to be either free or half-constrained lines
    ## after optimization
    ## so if the line endpoint is just a vertex, change its coordinates  
    ## otherwise the line endpoint might be a tick, change the t value
    Given:
        Items : [ {'index': int, 
                       'start': {'x': float, 'y': float, 'z': float},
                       'end': {'x': float, 'y': float, 'z': float}
                      }
                    ...
                ]
        state : 
    Return:
    '''

    pointsData = state['points']
    linesData = state['lines']

    points = all_points_positions( pointsData )
    lines = all_lines_positions( linesData, points )

    changed_edges_indices = [ ]

    live_vertex_indices = [ ]

    #
    changed_tick_point_positions = {}


    for item in input_data['Items']:
        line_index = item['index']
        start_point_index, end_point_index, line_type = linesData[line_index]

        start_x = item['start']['x'] 
        start_y = item['start']['y'] 
        start_z = item['start']['z'] 

        end_x = item['end']['x'] 
        end_y = item['end']['y'] 
        end_z = item['end']['z'] 

        new_start = [start_x, start_y, start_z]
        new_end = [end_x, end_y, end_z]

        old_start = node_i_position(start_point_index, pointsData)
        old_end =  node_i_position(end_point_index, pointsData)

        print()
        print('moved line_index', line_index)
        print('line_type', line_type.upper())
        print('start_point_index, end_point_index', start_point_index, end_point_index)
        print('old_start, old_end', old_start, old_end )
        print('moved_start, moved_end', new_start, new_end)
        print()

        changed_edges_indices.append( line_index )

        if node_is_tick(pointsData, start_point_index):
            changed_tick_point_positions[ start_point_index ] = new_start
        else:
            pointsData[start_point_index][:3] = new_start

        if node_is_tick(pointsData, end_point_index):
            changed_tick_point_positions[ end_point_index ] = new_end
        else:
            pointsData[end_point_index][:3] = new_end

        live_vertex_indices.append( start_point_index )
        live_vertex_indices.append( end_point_index )

    print('changed_edges_indices : ', changed_edges_indices)
    print('changed_tick_point_positions : ', changed_tick_point_positions)


    free_lines_indices =  find_free_lines_indices(linesData)


    moved_points = all_points_positions(pointsData)
    
    # also update the tick points positions 
    for key, value in changed_tick_point_positions.items():
        moved_points[key] = value

    print('moved_points : ',moved_points)


    moved_lines = all_lines_positions(linesData, moved_points)

    print('moved_lines : ', moved_lines)
    all_previous_constraints = calculate_constraints( lines , free_lines_indices )
    constraints_remove_broken_ones = remove_broken_constraints( all_previous_constraints, moved_lines )
    updated_edges_indices = find_updated_edges_indices( moved_lines, lines, free_lines_indices)

    # print('find_updated_edges_indices in c3', updated_edges_indices)
    constraints_in_moved_state = find_new_constraints(moved_lines, updated_edges_indices, changed_edges_indices, free_lines_indices)
    c_all = add_two_constraints(constraints_remove_broken_ones, constraints_in_moved_state)


 

    optimized_points = IRLS( c_all, moved_lines, moved_points, linesData, live_vertex_indices )
    print('optimized_points : ', optimized_points)

    # update all the free line optimized endpoints
    for live_vertex_index in live_vertex_indices:
        if live_vertex_index not in changed_tick_point_positions.keys():
            state['points'][live_vertex_index][:3] = optimized_points[ live_vertex_index ]

    print('state after updating vertex points',  state)

    print('changed_tick_point_positions', changed_tick_point_positions )

    # update the tick point
    for tick_index in changed_tick_point_positions:
        changed_tick_point_positions[tick_index] = optimized_points[ tick_index ]

    print('changed_tick_point_positions', changed_tick_point_positions )

    # update for the tick
    for key, val in changed_tick_point_positions.items():
        t = project_point_on_line( pointsData, optimized_points, key, optimized_points[key])
        state['points'][key][0] = t

    state['input_data'] = input_data['Items']

    # which point posiitions has been changed
    points_positions_changed = []
    for i in range(len(moved_points)):
        if np.allclose( points[i], moved_points[i], atol = 1e-3):
            continue
        else:
            points_positions_changed.append( i )


    optimize_curves( state, points_positions_changed )

    return state

def project_point_on_line( pointsData, points, point_index, point_new_pos):
    '''

    project the tick point on 
    '''

    # on which 2 points the point lerp on
    tick_lerp_index_0 = pointsData[point_index][1]
    tick_lerp_index_1 = pointsData[point_index][2]

    # print('point_index, tick_lerp_index_0, tick_lerp_index_1', point_index, tick_lerp_index_0, tick_lerp_index_1)

    start_position = points[tick_lerp_index_0]
    end_position = points[tick_lerp_index_1]

    # print('start_position, end_position', start_position, end_position )

    # print('point_new_pos, start_position, end_position', point_new_pos, start_position, end_position)

    return project_point_on_line_as_t( point_new_pos, [start_position, end_position])





def move_detail( input_data, state ):
    '''
    Given:
        Items : [  { 'index' : int,
                      't' : float
                    }
                    ...
                  ]
        state : ...
    '''
    # original points positions
    pointsData = state['points']
    points = all_points_positions( pointsData )


    points_positions_changed = []

    for item in input_data['Items']:
        index = item['index']
        t = item['t']

        points_positions_changed.append( index )

        # change the tick point t
        state['points'][index][0] = t



    state['input_data'] = input_data['Items']

    optimize_curves(state, points_positions_changed)

    return state





def optimize_curves( state, points_positions_changed ):
    '''
    state: already updated
    '''


    pointsData = state['points']
    linesData = state['lines']
    curvesData = state['curves']


    # here re-calculate point positions are needed
    # because some points are just ticks
    # and their endpoints gets updated
    # so I just use this to recompute all
    # if I want to save computation
    # I can do a loop on the tick and lerp on the optimized points
    optimized_points = all_points_positions( pointsData )
    lines = all_lines_positions( linesData, optimized_points )

    # 
    lines_directions = [ ]
    for line in lines:
        p0, p1 = line
        lines_directions.append( dir(p0, p1) )


    # print('optimized_points',optimized_points)
    # print('points_positions_changed', points_positions_changed)


    for curve_index in range(len(curvesData)):
        curve_info = curvesData[curve_index]
        if curve_info['type'] == 'straight_line':
            # do nothing if straight line
            pass
        else:
            # not all curve needs to reoptimize
            # only those with the key point position change 
            # is this true
            curve_need_update = False
            key_points_indices = curvesData[curve_index]['points']
            key_tangent_weights = curvesData[curve_index]['tangents']

            for index in key_points_indices:
                if index in points_positions_changed:
                    curve_need_update = True

            if curve_need_update:
                key_points = [optimized_points[index] for index in key_points_indices]

                key_tangents = []

                for tangent_index in range(len(key_tangent_weights)):
                    point_tangent = np.zeros(3)
                    for line_index, line_weight in key_tangent_weights[tangent_index]:
                        point_tangent += lines_directions[line_index] * line_weight

                    point_tangent = np.asarray( point_tangent )
                    # normailize the tangent
                    point_tangent = point_tangent / np.linalg.norm(point_tangent)

                    key_tangents.append( point_tangent )


                # print('key_points, key_tangents, key_magnitudes', key_points, key_tangents)

                optimized_magnitudes = reoptimize_curve.MVC_magnitudes(key_points, key_tangents)
                # curvesData[i]['magnitudes'] = optimized_magnitudes.tolist()
                curvesData[curve_index]['magnitudes'] = optimized_magnitudes






  
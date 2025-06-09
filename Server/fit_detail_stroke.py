import numpy as np
from scipy import interpolate


from l1regls import l1regls
import cvxopt


## Helpers
def resample( points, inc = 0.01 ):
    '''
    Given: 
        points: [P0, P1, ..., Pn-1] raw points
    Returns:
        resampled points, 0.01 cm per point
    '''
    points = np.asarray(points)

    n_points, dim = points.shape

    # Parametrization parameter s.
    dp = np.diff(points, axis=0)                 # difference between points
    dp = np.linalg.norm(dp, axis=1)              # distance between points
    d = np.cumsum(dp)                            # cumsum along the segments
    d = np.hstack([[0],d])                       # add distance from first point
    length = d[-1]                               # length of point sequence
    num = int(length/inc) +1                     # number of samples
    s = np.linspace(0,length,num)                # sample parameter and step


    # Compute samples per dimension separately.
    q = np.zeros([num, dim])
    for i in range(dim):
        q[:,i] = np.interp(s, d, points[:, i])
    return q
    
def extract_free_points_from_state( state ):
    '''
    given:
        state
    return:
        free_points
    '''
    points = []
    
    for x,y,z,t in state['points']:
        if t == 'vertex':
            points.append( [x, y, z])
    return points

def q_to_P_weight( q , P ):
    '''
    P : 4 * n
    q : 4 * 1

    return : n * 1 or n
    '''
    q = np.asarray( q )
    q = q.reshape(-1, 1)



    assert( q.shape == (4, 1))

    ds = np.linalg.norm( q - P, ord = 1, axis = 0)
    ds = ds / ds.sum()
    diagD = np.eye( P.shape[1] ) * 1 / ds

    A = P @ diagD

    A_mat = cvxopt.matrix( A )
    b_mat = cvxopt.matrix( q )

    W = l1regls( 1000 * A_mat , 1000 * b_mat)

    w_arr = np.array( W )
    w_arr = w_arr.reshape( P.shape[1] )
    w_arr = w_arr / ds 

    return w_arr.squeeze() 


def Q_to_P_W( Q, P ):
    '''
    P : n * 4 
    Q : m * 4

    W : n * m

    ----
    
    P.T : 4 * n 
    Q.T : 4 * m

    W : n * m

    P.T @ W = Q.T
    '''

    P = P.T

    W = []
    for q in Q :
        w = q_to_P_weight( q, P )
        W.append( w.tolist() )

    return W 



def extract_resampled_points( stroke_data ):
    '''
    given:
        stroke_data
    return:
        resampled stroke points
    '''

    scale = stroke_data['scale']
    pts = stroke_data['points']

    xi = [ pt['x'] for pt in pts]
    yi = [ pt['y'] for pt in pts]
    zi = [ pt['z'] for pt in pts] 

    curve_points = np.zeros([ len(pts) , 3])

    curve_points[:, 0] = xi
    curve_points[:, 1] = yi
    curve_points[:, 2] = zi

    points = resample( curve_points )

    return points


def fit_stroke_with_b_spline( points ):
    '''
    Given:
        3d points
    Return:
        t : knots
        c : control points
        k : degree
        n : number of points
    '''

    ## 1. get the xs, ys, zs of the points
    xs = points[:, 0]
    ys = points[:, 1]
    zs = points[:, 2]


    ## 2. fit a bspline of the stroke
    # s control the smooth
    # maybe I can calculate the spline length and adjust s
    # set s = 0.001 for now
    try:
        tck, u = interpolate.splprep([xs, ys, zs], k = 3, s = 0.001)
    except:
        try:
            tck, u = interpolate.splprep([xs, ys, zs], k = 2, s = 0.001)
        except:
            try:
                tck, u = interpolate.splprep([xs, ys, zs], k = 1, s = 0.001)
            except:
                # I don't think this step will happen
                # but in case 
                return None, None, None, None 

    t, c, k = tck
    # n how many points
    n = len(xs)

    c = np.asarray(c).T
    # print("c.shape", c.shape)

    return t, c, k, n


def control_points_on_free_points_weights( free_points, control_points ):
    '''
    given:
        free_points 
        control_points 
    Return :
        W : weights
    '''
    free_points = np.asarray( free_points )
    ones = np.ones((len(free_points),1))
    P = np.hstack( (free_points, ones) )


    ones = np.ones((len(control_points),1))
    Q = np.hstack( (control_points, ones) )

    w = Q_to_P_W( Q,  P )

    return w


def stroke_data_to_weights( stroke_data, state ):
    '''
    '''

    ### 1. resample the stroke points.
    ### 2. fit spline to the resampled points
    ### 3. turn spline control points to weights on free points
    ###    t, c, k, n : knots, control points, degree, number of stroke points
    ###    get from step 2. 


    # to protect: in case the stroke is too short 
    curve_points = extract_resampled_points( stroke_data )
    if len(curve_points) < 2:
        return 

    # to protect: in case the stroke is too short 
    t, c, k, n = fit_stroke_with_b_spline( curve_points )
    if c is None:
        return 

    free_points = extract_free_points_from_state( state )

    w = control_points_on_free_points_weights( free_points, c )

    # dictionary data for the stroke
    d = {}
    d['knots'] = t.tolist()
    d['weights'] = w
    d['degree'] = k
    d['n'] = n

    # add the resampled curve points to state
    state['stroke_points'].append( curve_points.tolist() )
    state['strokes'].append( d )




# reoptimize curve : change the magnitudes of curve tangents
# to minimize the curvature

import numpy as np
from scipy.optimize import minimize



## helper
M = np.asfarray([
    [ -1, 3, -3, 1 ],
    [ 3, -6, 3, 0 ],
    [ -3, 3, 0, 0 ],
    [ 1, 0, 0, 0 ]])

## Optimize Helpers
def bezier_curve( ts, c0, c1, c2, c3 ):
    '''
    Given:
        ts: a sequence of t values to evaluate the spline
        c0, c1, c2, c3: d-dimensional values specifying the four control points of the spline.
    Returns:
        points: Returns a sequence of d-dimensional points, each of which is the cubic spline evaluated at each t in `ts`.
            Returns the sequence as a `len(ts)` by d numpy.array.
    '''
    
    P = np.zeros( ( 4, len( c0 ) ) )
    P[0] = c0
    P[1] = c1
    P[2] = c2
    P[3] = c3
    Ts = np.tile( ts, ( 4, 1 ) ).T
    # print(np.asfarray(ts).shape)
    # print(Ts.shape)
    Ts[:,0] **= 3
    Ts[:,1] **= 2
    Ts[:,2] **= 1
    Ts[:,3] = 1
    return Ts @ (M @ P)

def evaluate(points, tangents, magnitudes, resolution = 0.01):
    '''
    Given:
        points  
        tangents
        magnitudes
    Return:
        spline points
    '''
    points = np.asarray(points)
    tangents = np.asarray(tangents)
    nPoints, dim = points.shape
    
    spline_points = np.empty((0,dim))

    k = 0
    
    for i in range(nPoints - 1):
        c0 = points[i]
        c3 = points[i+1]
        c1 = c0 + tangents[i] * magnitudes[k]
        c2 = c3 - tangents[i+1] * magnitudes[k+1]
        
        k = k + 2
        
        d = np.linalg.norm(c3-c0)
        
        # max(2) because we always want to sample at least once in the middle
        ts = np.linspace(0, 1, max( 2, int(d/resolution) ), endpoint= False)
        # add endpoint for the last segment
        if i == nPoints - 2:
            ## Add 1 for the second endpoint
            ts = np.linspace(0, 1, max( 2, int(d/resolution) ) + 1, endpoint=True)
        
        curve_points = bezier_curve(ts, c0, c1, c2, c3)
        spline_points = np.concatenate((spline_points, curve_points))
        

    return spline_points

def init_X(points):
    '''
    Given:
        points
    Return:
        X
    '''
    # init X 
    X  = np.ones(len(points) * 2 - 2) 
    
    # init X value to avoid self intersections
    k = 0
    for i in range(len(points)-1):
        p0 = points[i]
        p1 = points[i+1]
        d = np.linalg.norm(p1-p0)
        X[k] = X[k+1] = d/3
        k = k + 2
    
    return X

def menger_curvature(pts):
    '''
    https://en.wikipedia.org/wiki/Menger_curvature
    Given:
        pts: A polyline of 3D points
    Returns:
        The menger curvature at pts[1], pts[2], ..., pts[-2]
    '''
    
    pts = np.asarray(pts)
    ## This only works in 2D or 3D
    assert pts.shape[1] in (2,3)
    
    xy = pts[:-2] - pts[1:-1]
    yz = pts[1:-1] - pts[2:]
    zx = pts[2:] - pts[:-2]
    
    a = ( xy**2 ).sum( 1 )
    b = ( yz**2 ).sum( 1 )
    c = ( zx**2 ).sum( 1 )
    
    area = ( np.cross( xy, zx ) ** 2 )
    ## In 2D, the cross product function returns the z component, which is the only non-zero element.
    if pts.shape[1] == 3: area = area.sum(1)
    result = 2 * np.sqrt( area / ( a * b * c) )
    result[ area < 1e-20 ] = 0.
    
    return result

def edge_weights(pts):
    '''
    Given:
        pts: A polyline of 3D points
    Returns:
        The mass weighting factor for each edge in `pts`.
        The result has length one less than `pts`.
    '''
    
    pts = np.asarray(pts)
    
    edges = pts[:-1] - pts[1:]
    edge_lengths = np.sqrt( ( edges**2 ).sum(1) )
    return edge_lengths

def property_along_curve( samples ):
    '''
    Given:
        samples: An array of many points
    Returns:
        variation_of_curvature: An array of variation-of-curvature values computed along the curve. It will have length one smaller.
    '''

    curvature = menger_curvature( samples )
    return curvature[1:] - curvature[:-1]

def loss( v ):
    return v**2

## Optimizer
def MVC_magnitudes(points, tangents, X = None ):
    '''
    Given:
        points: A sequence of N d-dimensional points to interpolate
        tangents: A sequence of d-dimensional tangent directions at each point
        magnitudes: A sequence of 2 * N - 2 floating numbers, magnitude of tangent
    Returns:
        points: A sequence of N d-dimensional points to interpolate
        tangents: A sequence of d-dimensional tangent directions at each point
        magnitudes: A sequence of 2 * N - 2 floating numbers, magnitude of tangent
    '''
    
    
    ### We want to optimize a property of the spline constructed
    ### from the sequence of points and tangents. The degrees of freedom
    ### are the magnitudes of those tangents.
    ### We need a function to evaluate the property we want to measure.
    ### The property in general can be positive or negative,
    ### We want to minimize some "loss" function of the property,
    ### like the absolute value, or the square, or a fancier function.
    ### In our case, we want to measure (and minimize) the variation in curvature.
    ### We could try for an analytic solution to all or part of this problem by,
    ### for example, computing the integral of the squared variation of curvature
    ### directly and then solving for the derivative equal to 0 or
    ### solving the Euler-Lagrange equations.
    ### In general, it will be difficult to do this, so we can approximate this
    ### by sampling everything.
    ### That means, we need:
    ### 1) a function to sample each piecewise curve: sample()
    ### 2) a function to compute the property we want to measure along the sampled curve (e.g. given a sample point and some of its neighbors): property( samplei-1 samplei samplei+1] )
    ### 3) a loss function: loss
    ### We also need a function to create piecewise curves given the current degrees-of-freedom: unpack()
    
    points = np.asarray( points )
    tangents = np.asarray( tangents )
    X = init_X(points)
    


    
    def f(X):
        evaluated_bezier_points = evaluate(points, tangents, X)
        prop = property_along_curve( evaluated_bezier_points )
        
        ## unweighted
        # E = sum( [loss( v ) for v in prop ])
        # E = loss(prop).sum()
        
        ## weighted
        E = ( loss(prop)*edge_weights( evaluated_bezier_points )[1:-1] ).sum()
        
        return E
    
    result = minimize( f, X, method = 'BFGS', jac = None, tol = 0.0001, options = { 'disp': False, 'eps': 0.000001, 'gtol': 0.0001, 'maxiter': 1000 } )

    # # also need to avoid cusp
    # # not sure whether this is a good way to do so
    # # maybe use init_X or the original X
    # # I do not want use the optimized result if the result.x is far from original
    # # 3 might create a cusp or self-intersection
    # if any( np.abs(result.x/X) > 3):
    #     print('no optimize happend')
    #     return X

    print('before optimization', X)
    print('optimized magnitudes', result.x)

    print('before optimization x value', np.linalg.norm(X))
    print('after optimization x value', np.linalg.norm(result.x))

    if np.linalg.norm( result.x) / np.linalg.norm(X) >= 10:
        return X.tolist()

    return result.x.tolist()
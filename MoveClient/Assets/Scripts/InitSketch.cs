using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.XR;

using NativeWebSocket;
using TMPro;


/// 
/// 
///
/// 4 Tags:
/// ScaffoldLine : all the scaffold lines
/// ScaffoldPoint : all the tick points
/// Curve : all the curve
/// Stroke : all the strokes
///
///
/// 4 Groups:
/// scaffoldGroup : all free, half-constrained lines ( most important, user created in the begining)
/// detailScaffoldGroup : constrained lines and tick points
/// curveGroup : all curves
/// strokeGroup : all strokes
///
///
/// I can either look by groups or find by tags
///
///
/// 4 Modes:
/// Draw : draw stroke mode
/// Move : show free and half_constrained scaffold lines
/// Detail : show tick points, but only allow select and move tick points
/// Curve : hide all scaffolds, hide scaffold, only show curves
///
///
/// detailMode : have a detailMode 
///  1. avoid messy 
///  2. since we won't change the topology of the sketch, the optimization should also be different, only change t value
///
///
///  This script attached to the GameController Object
/// 


public class InitSketch : MonoBehaviour
{

    ///
    /// for multiple selection
    /// 
    ///
    GameObject[] scaffoldLines;
    GameObject[] scaffoldPoints;
    GameObject[] curves;
    GameObject[] strokes;

    List<GameObject> selectedObjects;


    private InputDevice rightController;
    private InputDevice leftController;

    private Quaternion rightControllerLastRotation;

    private Vector3 rightControllerLastPosition;
    private Vector3 leftControllerLastPosition;

    // for button presse/touch
    private bool rightLastGripState = false;
    private bool leftLastGripState = false;
    private bool rightLastSecondaryState = false;



    // for drawing stroke
    private bool rightLastTriggerState = false;
    private LineRenderer currLine;
    private int numClicks;

    private GameObject rawStrokeObject;

    public enum Mode
    {
        Draw,
        Move,
        Detail,
        Curve,
    }

    private Mode currentMode = Mode.Move;

    private GameObject scaffoldGroup;
    private GameObject curveGroup;
    
    private GameObject detailScaffoldGroup;

    private GameObject strokeGroup;

    [SerializeField]
    private Animator rightHandAnimator;

    [SerializeField]
    private Animator leftHandAnimator;




    // Awake is called when the script object is initialised, regardless of whether or not the script is enabled.
    void Awake()
    {

        List<InputDevice> rightHandDevices = new List<InputDevice>();
        InputDeviceCharacteristics rightControllerCharacteristics = InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller;
        InputDevices.GetDevicesWithCharacteristics(rightControllerCharacteristics, rightHandDevices);

        foreach (var item in rightHandDevices)
        {
            Debug.Log( item.name + item.characteristics);
        }

        if (rightHandDevices.Count > 0)
        {
            rightController = rightHandDevices[0];
        }

        List<InputDevice> leftHandHandDevices = new List<InputDevice>();
        InputDeviceCharacteristics leftControllerCharacteristics = InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller;
        InputDevices.GetDevicesWithCharacteristics(leftControllerCharacteristics, leftHandHandDevices);

        foreach (var item in leftHandHandDevices)
        {
            Debug.Log( item.name + item.characteristics);
        }

        if (leftHandHandDevices.Count > 0)
        {
            leftController = leftHandHandDevices[0];
        }

        stateParser = new StateParser();

        
        
        scaffoldGroup = new GameObject();
        scaffoldGroup.name = "scaffoldGroup";   

        curveGroup = new GameObject();
        curveGroup.name = "curveGroup";

        detailScaffoldGroup = new GameObject();
        detailScaffoldGroup.name = "detailScaffoldGroup";

        strokeGroup = new GameObject();
        strokeGroup.name = "strokeGroup";



    }

    private void UpdateHandAnimation()
    {
        // right 
        if (rightController.TryGetFeatureValue(CommonUsages.trigger, out float rightTriggerValue))
        {
            rightHandAnimator.SetFloat("Trigger", rightTriggerValue);
        } 
        else 
        {
            rightHandAnimator.SetFloat("Trigger", 0);
        }

        if (rightController.TryGetFeatureValue(CommonUsages.grip, out float rightGriprValue))
        {
            rightHandAnimator.SetFloat("Grip", rightGriprValue);
        } 
        else 
        {
            rightHandAnimator.SetFloat("Grip", 0);
        }

        // left
        if ( leftController.TryGetFeatureValue(CommonUsages.trigger, out float leftTrigerValue))
        {
            leftHandAnimator.SetFloat("Trigger", leftTrigerValue);
        } 
        else
        {
            leftHandAnimator.SetFloat("Trigger", 0);
        }

        if ( leftController.TryGetFeatureValue(CommonUsages.grip, out float leftGripValue))
        {
            leftHandAnimator.SetFloat("Grip", leftGripValue);
        }
        else
        {
            leftHandAnimator.SetFloat("Grip", 0);
        }


    }

  


    // Update is called once per frame
    void Update()
    {
        UpdateHandAnimation();

        ///
        ///
        /// Mode: Draw -> Move -> Detail -> Curve -> Draw -> ...
        /// In Draw mode, trigger button to draw 
        ///
        ///
        /// 

        if (currentMode == Mode.Draw)
        {
            /// right button trigger : draw

            
            // https://docs.unity3d.com/Manual/xr_input.html#AccessingInputFeatures
            // the right controller trigger button 
            bool rightTempTriggerState = false;
            bool rightTriggerButtonState = false;

            rightTempTriggerState = rightController.TryGetFeatureValue(CommonUsages.triggerButton, out rightTriggerButtonState)  
                            && rightTriggerButtonState
                            || rightTempTriggerState;
                
            if ( rightTempTriggerState != rightLastTriggerState)
            {
                // trigger button: first pressed
                if (rightTempTriggerState == true)
                {
                    // Debug.Log("right grip button first pressed");
                    rightController.TryGetFeatureValue(CommonUsages.devicePosition, out rightControllerLastPosition); 


                    rawStrokeObject = new GameObject();
                    currLine = rawStrokeObject.AddComponent<LineRenderer>();
                    currLine.material = new Material(Shader.Find("Sprites/Default"));
                    currLine.material.SetColor("_Color", new Color(0f, 0f, 1f, 0.8f));

                    currLine.startWidth = curve_width;
                    numClicks = 0;

                } 
                else
                {
                    // Grip button : relased
                    // Debug.Log("grip button first released.");
                    
                    Vector3[] points = new Vector3[numClicks];
                    currLine.GetPositions(points);

                    var data = new Data();
                    data.scale = Vector3.one;
                    data.points = points;

                    string text = JsonUtility.ToJson( data );


                    SendWebSocketMessage("detail-stroke " + text);
                }

                rightLastTriggerState = rightTempTriggerState;
            }






            /// right trigger button: begin to draw
            rightController.TryGetFeatureValue( CommonUsages.triggerButton, out bool triggerButtonValue);
            if (triggerButtonValue)
            {

                rightController.TryGetFeatureValue(CommonUsages.devicePosition, out rightControllerLastPosition); 

                // var penBallPosition = rightControllerLastPosition + new Vector3(0.0f, 0.0f, 0.08f);
                // penBallPosition = rightControllerLastPosition;

                var penBallPosition = penBall.transform.position;

                // Debug.Log( "penBall.transform.world postion" +  penBall.transform.position );
                // Debug.Log( "controller postions " +  rightControllerLastPosition );
                // Debug.Log(" difference " + (penBall.transform.position - rightControllerLastPosition));



                currLine.positionCount = numClicks + 1;
                currLine.SetPosition( numClicks, penBallPosition);
                numClicks++;
            }

                

            
        }
        else 
        {

            ///
            /// right trigger button: clear the selection
            /// 
            rightController.TryGetFeatureValue(CommonUsages.triggerButton, out bool triggerButtonValue);
            if (triggerButtonValue)
            {
                Debug.Log("right controller trigger button clear");
                FindSelectedObjects();
                ClearSelection();
            }

            // https://docs.unity3d.com/Manual/xr_input.html#AccessingInputFeatures
            // the right controller grip button is used to indicate start move and end move
            bool rightTempGripState = false;
            bool rightGripButtonState = false;

            rightTempGripState = rightController.TryGetFeatureValue(CommonUsages.gripButton, out rightGripButtonState)  
                            && rightGripButtonState
                            || rightTempGripState;
                
            if ( rightTempGripState != rightLastGripState)
            {
                // Grip button: first pressed
                if (rightTempGripState == true)
                {

                    // Debug.Log("right grip button first pressed");
                    rightController.TryGetFeatureValue(CommonUsages.devicePosition, out rightControllerLastPosition);

                    // find selected object every time I press grip button
                    FindSelectedObjects();       
                } 
                else
                {
                    // Grip button : relased
                    // Debug.Log("grip button first released.");
                    if ( selectedObjects.Count > 0 )
                    {   
                        MovedLinesInformation();  
                        SendMovedObjectsInformation();      
                        ClearSelection();
                        Optimize_in_process_TMP.SetActive( true );
                        opt_start_time = Time.time;
                    }
                }

                rightLastGripState = rightTempGripState;
            }

            if (currentMode == Mode.Move )
            {    

                // right hand: suppose user is right handed
                // move + scale 
                // 1. right hand grip first pressed -> record the location of right hand
                // 2. right hand grip being pressed -> selected points following the controller translation 
                // 3. right hand grip being pressed, left hand grip being pressed  ->  scale happens
                // 4. right hand grip released  -> clear selection and send the data to back end to optimize
                // 
                // treat rotate differently 
                // I don't like rotation + movement
                // b button is the secondary button on the controller 
                // rotate:
                // 
                // 5. right hand b button first touched -> record the rotation of right hand 
                // 6. right hand b button being touched -> the selected points rotation following right controller
                // 7. right hand b button released -> clear selection and send the data to back end to optimize
                // 


                bool leftTempGripState = false;
                bool leftGripButtonState = false;

                leftTempGripState = leftController.TryGetFeatureValue(CommonUsages.gripButton, out leftGripButtonState)  
                                && leftGripButtonState
                                || leftTempGripState;
                    
                if ( leftTempGripState != leftLastGripState)
                {
                    if (leftTempGripState == true && rightTempGripState == false)
                    {
                        // FindSelectedObjects();       

                        // Debug.Log("left grip button first pressed");
                        leftController.TryGetFeatureValue(CommonUsages.devicePosition, out leftControllerLastPosition); 
                    } 
                    leftLastGripState = leftTempGripState;
                }


                // the right controller B button
                // to get right controller rotation when first touched
                bool rightTempSecondaryState = false;
                bool rightSecondaryButtonState = false;

                rightTempSecondaryState = rightController.TryGetFeatureValue(CommonUsages.secondaryTouch, out rightSecondaryButtonState)  
                                && rightSecondaryButtonState
                                || rightTempSecondaryState;
                    
                if ( rightTempSecondaryState != rightLastSecondaryState)
                {
                    if (rightTempSecondaryState == true)
                    {
                        // Debug.Log("right Secondary button first pressed");
                        rightController.TryGetFeatureValue(CommonUsages.deviceRotation, out rightControllerLastRotation); 

                        // find selected object every time I press grip button
                        FindSelectedObjects();   

                    } 
                    else
                    {
                        // Debug.Log("grip button first released.");
                        if ( selectedObjects.Count > 0 )
                        {   
                            MovedLinesInformation();  
                            SendMovedObjectsInformation();      
                            ClearSelection();
                            Optimize_in_process_TMP.SetActive( true );
                            opt_start_time = Time.time;
                        }
                        
                        // open the fist when 'b button' released
                        rightHandAnimator.SetFloat("Grip", 0);    

                    }
                    rightLastSecondaryState = rightTempSecondaryState;
                }



                //
                rightController.TryGetFeatureValue( CommonUsages.gripButton, out bool rightGripButtonValue);
                leftController.TryGetFeatureValue( CommonUsages.gripButton, out bool leftGripButtonValue);
                rightController.TryGetFeatureValue( CommonUsages.secondaryTouch, out bool rightSecondaryButton );     

                // right grip button being pressed
                if ( rightGripButtonValue && !leftGripButtonValue )
                {

                    // Debug.Log("move");
                    if (selectedObjects.Count > 0)
                    {
                        // avoid any other scaffolds being selected when the hand is moving some objects
                        DisallowScaffoldsSelection();

                        rightController.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 rightControllerCurrentPosition);

                        Vector3 translation = rightControllerCurrentPosition - rightControllerLastPosition;                        


                        // 1. Create a new dictionary of the selected points, with original value.
                        Dictionary<int, Vector3> actualSelectedPoints = new Dictionary<int, Vector3>();

                        for (int i = 0; i < selectedObjects.Count; i++)
                        {
                            var lineIndex = int.Parse( selectedObjects[i].name );
                            
                            var startPointIndex = state.lines[lineIndex].startIndex;
                            var endPointIndex = state.lines[lineIndex].endIndex;

                            

                            actualSelectedPoints[startPointIndex] = state.point_i_position(startPointIndex);
                            actualSelectedPoints[endPointIndex] = state.point_i_position(endPointIndex);


                            



                        }

                        // https://stackoverflow.com/questions/2260446/how-to-iterate-through-dictionary-and-change-values
                        // 2. move the actual selected points
                        var keys = new List<int>(actualSelectedPoints.Keys);
                        foreach(var key in keys)
                        {
                            actualSelectedPoints[key] = actualSelectedPoints[key] + translation;
                        }
                                                                

                        // 3. update the state
                        foreach(var key in keys)
                        {
                            if (state.points[key].type == "tick")
                            {
                                      
                            
                                var point = state.points[key];


                                // The point is a tick
                                // so t, i0, i1 is the point 
                                var i0 =  point.i0;
                                var i1 =  point.i1;
                                var p0 = state.point_i_position( i0 );
                                var p1 = state.point_i_position( i1 );                        
                                var p = state.point_i_position( key );
                                var l = p1 - p0;

                                // https://docs.unity3d.com/ScriptReference/Vector3.Project.html
                                var projectTranslationOnLine = Vector3.Project(translation, p - p0);
                                
                                var newPoint = p + projectTranslationOnLine;
                                // Do I need to assure this new Point's 0 < t < 1       

                                // The new t value can be calculated from the 
                                var t = point.x; // remember point is a tick, and x is the t value
                                var deltaT = Vector3.Dot(projectTranslationOnLine, l) / l.sqrMagnitude;
                                var newT = Mathf.Clamp(t + deltaT, 0.0f, 1.0f);
                            
                                // Debug.Log("deltaT " + deltaT.ToString());
                                // Debug.Log( "newT" + newT.ToString());

                                // selectedObjects[key].transform.position += projectTranslationOnLine;

                                // set the tick point new t
                                state.move_point(key, Vector3.zero, newT);
                            }
                            else
                            {
                                state.move_point(key, actualSelectedPoints[key]);
                            }
                        }


                        UpdateUIElements();

                        rightControllerLastPosition = rightControllerCurrentPosition;
                    }
                } 
                else if ( rightGripButtonValue && leftGripButtonValue )
                {
                    // Debug.Log("scale");
                
                
                    if (selectedObjects.Count > 0)
                    {
                        // avoid any other scaffolds being selected when the hand is moving some objects
                        DisallowScaffoldsSelection();


                        // only apply scale when both controller pressed
                        rightController.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 rightControllerCurrentPosition);
                        leftController.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 leftControllerCurrentPosition);

                        // Create a new dictionary of the selected points, with original value.
                        Dictionary<int, Vector3> actualSelectedPoints = new Dictionary<int, Vector3>();
                        for (int i = 0; i < selectedObjects.Count; i++)
                        {
                            var lineIndex = int.Parse( selectedObjects[i].name );
                            
                            var startPointIndex = state.lines[lineIndex].startIndex;
                            var endPointIndex = state.lines[lineIndex].endIndex;


                            actualSelectedPoints[startPointIndex] = state.point_i_position(startPointIndex);
                            actualSelectedPoints[endPointIndex] = state.point_i_position(endPointIndex);

                        }

                        // Calculate the centroid
                        Vector3 centroid = Vector3.zero;
                        var keys = new List<int>(actualSelectedPoints.Keys);
                        foreach(var key in keys)
                        {
                            centroid += actualSelectedPoints[key];
                        }

                        centroid /= keys.Count;

                        // scale factor
                        Vector3 previous_PQ = (rightControllerLastPosition - leftControllerLastPosition);
                        Vector3 PQ = (rightControllerCurrentPosition - leftControllerCurrentPosition);
                        float m0 = PQ.magnitude / previous_PQ.magnitude ;
                        var scaleM = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one * m0);


                        // scale the points
                        foreach(var key in keys)
                        {
                            actualSelectedPoints[key] = scaleM.MultiplyPoint3x4( actualSelectedPoints[key] - centroid)  + centroid;
                        }

                        foreach(var key in keys)
                        {
                            state.move_point(key, actualSelectedPoints[key]);
                        }

                        UpdateUIElements();

                        rightControllerLastPosition = rightControllerCurrentPosition;
                        leftControllerLastPosition = leftControllerCurrentPosition;
                    }
                }

                // right b button for rotate
                if ( rightSecondaryButton )
                {
                    // make a fist when 'b button' touched
                    rightHandAnimator.SetFloat("Grip", 1);    

                    // only apply rotation when left controller pressed
                    if (selectedObjects.Count > 0)
                    {
                        // broken here
                        // avoid any other scaffolds being selected when the hand is moving some objects
                        DisallowScaffoldsSelection();

                        rightController.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rightControllerCurrentRotation);
                        Quaternion rotation = rightControllerCurrentRotation * Quaternion.Inverse(rightControllerLastRotation);

 

                        // rotate along the center
                        // 1. find the scale center
                        Dictionary<int, Vector3> actualSelectedPoints = new Dictionary<int, Vector3>();

                        for (int i = 0; i < selectedObjects.Count; i++)
                        {
                            var lineIndex = int.Parse( selectedObjects[i].name );
                            
                            var startPointIndex = state.lines[lineIndex].startIndex;
                            var endPointIndex = state.lines[lineIndex].endIndex;

                            actualSelectedPoints[startPointIndex] = state.point_i_position(startPointIndex);
                            actualSelectedPoints[endPointIndex] = state.point_i_position(endPointIndex);

                        }

                        var keys = new List<int>(actualSelectedPoints.Keys);
                        Vector3 pointsAvg = Vector3.zero;


                        foreach (var key in keys)
                        {
                            pointsAvg += actualSelectedPoints[key];
                        }
                        pointsAvg /= actualSelectedPoints.Count;

                        foreach(var key in keys)
                        {
                            actualSelectedPoints[key] = rotation * (actualSelectedPoints[key] - pointsAvg) + pointsAvg;

                        }


                        // 3. update the state
                        foreach(var key in keys)
                        {
                            state.move_point(key, actualSelectedPoints[key]);
                        }

                        
                        UpdateUIElements();

                        rightControllerLastRotation = rightControllerCurrentRotation;
                        
                    }
                }


                
            } 
            // Detail Mode
            else if ( currentMode == Mode.Detail)
            {
                rightController.TryGetFeatureValue( CommonUsages.gripButton, out bool rightGripButtonValue);
                if (rightGripButtonValue)
                {

                    rightController.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 rightControllerCurrentPosition);

                    var translation = rightControllerCurrentPosition - rightControllerLastPosition;

                    if ( selectedObjects.Count > 0)
                    {

                        // avoid any other scaffolds being selected when the hand is moving some objects
                        DisallowScaffoldsSelection();

                        // The selected detail scaffolds are tick points
                        for (int i = 0; i < selectedObjects.Count; i++)
                        {

                            var pointIndex = int.Parse( selectedObjects[i].name.ToString() );
                            var point = state.points[pointIndex];


                            // The point is a tick
                            // so t, i0, i1 is the point 
                            var i0 =  point.i0;
                            var i1 =  point.i1;
                            var p0 = state.point_i_position( i0 );
                            var p1 = state.point_i_position( i1 );                        
                            var p = state.point_i_position( pointIndex );
                            var l = p1 - p0;

                            // https://docs.unity3d.com/ScriptReference/Vector3.Project.html
                            var projectTranslationOnLine = Vector3.Project(translation, p - p0);
                            
                            var newPoint = p + projectTranslationOnLine;
                            // Do I need to assure this new Point's 0 < t < 1       

                            // The new t value can be calculated from the 
                            var t = point.x; // remember point is a tick, and x is the t value
                            var deltaT = Vector3.Dot(projectTranslationOnLine, l) / l.sqrMagnitude;
                            var newT = Mathf.Clamp(t + deltaT, 0.0f, 1.0f);
                        
                            // Debug.Log("deltaT " + deltaT.ToString());
                            // Debug.Log( "newT" + newT.ToString());

                            selectedObjects[i].transform.position += projectTranslationOnLine;

                            // set the tick point new t
                            state.move_point(pointIndex, Vector3.zero, newT);
                        }

                        UpdateUIElements();
                        rightControllerLastPosition = rightControllerCurrentPosition;
                    }

                } 
                
            }


         

        }


        leftController.TryGetFeatureValue( CommonUsages.primaryButton, out bool leftPrimaryButtonValue);
        if (leftPrimaryButtonValue){
            SetCurveMode();
        }


 

        // for socket
        #if !UNITY_WEBGL || UNITY_EDITOR
            websocket.DispatchMessageQueue();
        #endif

    }

    private void FindSelectedObjects()
    {
        selectedObjects = new List<GameObject>();

        // Not sure which one is faster
        // FindGameObjectsWithTag or Find the child
        // Do not think the performance matters here
        // also, this is cause find every time 
        // maybe I can move it some other places
        scaffoldLines = GameObject.FindGameObjectsWithTag("ScaffoldLine");
        scaffoldPoints = GameObject.FindGameObjectsWithTag("ScaffoldPoint");


        // selectedObjects can only have free line or tick points
        // not two of them at the same time 
        foreach (var item in scaffoldLines)
        {
            if (item.GetComponent<ChangeColorWhenCollide>().isSelected)
            {
                selectedObjects.Add( item.gameObject );
            }
        }

        foreach (var item in scaffoldPoints)
        {
            if (item.GetComponent<ChangeColorWhenCollide>().isSelected)
            {
                selectedObjects.Add( item.gameObject );
            }
        }

        //print the size of selected Objects
        // Debug.Log("selected object size " + selectedObjects.Count);
    }

    //
    // clear selection:
    // 1. the selected objects deselected 
    // 2. everything can be selected again
    //  
    private void ClearSelection()
    {

        foreach (GameObject item in selectedObjects)
        {
            item.GetComponent<ChangeColorWhenCollide>().isSelected = false;
        }

        selectedObjects = new List<GameObject>();
    }


    // Not allow scaffolds to be selected
    private void DisallowScaffoldsSelection()
    {
        foreach (var item in scaffoldLines)
        {
            item.GetComponent<ChangeColorWhenCollide>().canBeSelected = false;
        }

        foreach (var item in scaffoldPoints)
        {
            item.GetComponent<ChangeColorWhenCollide>().canBeSelected = false;
        }
        
    }


    // Allow Scaffolds for selection
    private void AllowScaffoldsSelection()
    {
        foreach (var item in scaffoldLines)
        {
            item.GetComponent<ChangeColorWhenCollide>().canBeSelected = true;
        }

        foreach (var item in scaffoldPoints)
        {
            item.GetComponent<ChangeColorWhenCollide>().canBeSelected = true;
        }
        
    }



    // print the selected line information
    private void MovedLinesInformation()
    {

        if ( currentMode == Mode.Move)
        {
            ScaffoldLineJSON[] scaffoldsInstance = new ScaffoldLineJSON[selectedObjects.Count];

            for (int i = 0; i < selectedObjects.Count; i++)
            {
                var line = selectedObjects[i];
                var line_name = selectedObjects[i].gameObject.name;
                var line_index = int.Parse( line_name );

                var line_start_point_index = state.lines[line_index].startIndex;
                var line_end_point_index = state.lines[line_index].endIndex;

                var start_position = state.points[line_start_point_index];
                var end_position = state.points[line_end_point_index];

                scaffoldsInstance[i] = new ScaffoldLineJSON(line_index, start_position.ToVector3(), end_position.ToVector3());
            }

            string scaffoldsToJson = JsonHelper.ToJson(scaffoldsInstance, true);

            // Debug.Log("scaffoldToJSON" + scaffoldsToJson);
        } 
        else
        {

            ScaffoldPointJSON[] tickPointsInstance = new ScaffoldPointJSON[selectedObjects.Count];

            for (int i = 0; i < selectedObjects.Count; i++)
            {
                var item = selectedObjects[i];
                var tick_index = int.Parse( item.gameObject.name );
                float t = state.points[tick_index].x;
                
                tickPointsInstance[i] = new ScaffoldPointJSON(tick_index, t);
            }

            string detailScaffoldsToJSON = JsonHelper.ToJson(tickPointsInstance, true);

            // Debug.Log("detailScaffoldsToJSON " + detailScaffoldsToJSON);
        }

        
    }



    /// 
    /// below are the scripts for the drawing and updating
    /// do this for performace ?
    ///

    [SerializeField]
    private GameObject cylinderPrefab;

    [SerializeField]
    private GameObject spherePrefab;

    // Text Mesh Pro : for UI Press
    [SerializeField]
    private TMP_Text current_mode_TMP;

    [SerializeField]
    private TMP_Text undo_redo_TMP;


    
    [SerializeField]
    private GameObject Optimize_in_process_TMP;
    
    [SerializeField]
    private GameObject penBall;

    // whenever Optimize_in_process_TMP change
    // record time
    // use this to get optimize time
    //
    private float opt_start_time;








    

    // [SerializeField]
    // private GameObject scaffoldGroup;

    // [SerializeField]
    // private GameObject curveGroup;
    
    // [SerializeField]
    // private GameObject detailScaffoldGroup;

    private float delay = 3.0f;

    // cylinder width : for drawing 
    private float width = 0.008f;
    private float curve_width = 0.01f;

    // to parse the state
    private State state;
    StateParser stateParser;




    // redraw state happens:
    // init, undo, redo
    // 
    // everytime I redraw everything
    // I also want to make sure things in the right state
    // 
    private void DrawState(State state)
    {

        // Destroy(scaffoldGroup);
        // Destroy(curveGroup);
        // Destroy(detailScffoldGroup);
        
        RemoveAllChildrenOfGroup( scaffoldGroup );
        RemoveAllChildrenOfGroup( curveGroup );
        RemoveAllChildrenOfGroup( detailScaffoldGroup );
        RemoveAllChildrenOfGroup( strokeGroup );



        DrawLines( state );     
        DrawCurves( state );
        // DrawTickPoints( state );
        DrawStrokes( state );
        
        // Can I just put this here ?
        // use this to cache all the scaffolds and detail scaffolds
        scaffoldLines = GameObject.FindGameObjectsWithTag("ScaffoldLine");
        // scaffoldPoints = GameObject.FindGameObjectsWithTag("ScaffoldPoint");
        curves = GameObject.FindGameObjectsWithTag("Curve");

    }

    // Set The current mode
    void SetCurrentMode()
    {
        if ( currentMode == Mode.Draw )
        {
            SetDrawMode();
        }  
        else if (currentMode == Mode.Move)
        {
            SetMoveMode();
        }
        // else if (currentMode == Mode.Detail) 
        // {
        //     SetDetailMode();
        // }
        // else
        // {
        //     SetCurveMode();
        // }
   
    }

    // Only draw tick points
    void DrawTickPoints(State state)
    {
        // only draw the tick points and add that to the detailScffoldGroup group
        for (int i = 0; i < state.points.Count; i++)
        {
            string pointType = state.points[i].type;

            if (pointType == "tick")
            {
                GameObject sphere = Instantiate(spherePrefab, state.point_i_position(i), Quaternion.identity);
                sphere.tag = "ScaffoldPoint";
                sphere.name = i.ToString();
                sphere.transform.parent = detailScaffoldGroup.transform;
            }
        }
    }

    void DrawLines(State state)
    {
        for (int i = 0; i < state.lines.Count; i++)
        {
            Vector3[] endpoints = state.line_i_endpoints(i);
            Vector3 start = endpoints[0];
            Vector3 end = endpoints[1];

            // Debug.Log( state.lines[i].type);

            DrawCylinderBetweenPoints(start, end, width ,i, state.lines[i].type);
        }
    }

    void DrawCurves(State state)
    {
        for (int i = 0; i < state.curves.Count; i++)
        {
            List<Vector3> curve_points = state.curve_i_points(i);
            DrawCurve( curve_points, curve_width, i , "Curve", curveGroup);
        }
    }

    void DrawStrokes(State state)
    {

        Debug.Log(" state.strokes.count : " + state.strokes.Count);

        
        for (int i = 0; i < state.strokes.Count; i++)
        {


            List<Vector3> stroke_points = state.stroke_i_points( i );
            DrawCurve( stroke_points, curve_width, i, "Stroke", strokeGroup);

            Debug.Log("stroke " + i + " points count " + stroke_points.Count);
        }
    }


    void DrawCylinderBetweenPoints(Vector3 start, Vector3 end, float width, int index, string type)
    {
        var offset = end - start;
        var scale = new Vector3(width, offset.magnitude / 2.0f , width);
        var position = start + (offset / 2.0f);

        
        GameObject cylinder = Instantiate(cylinderPrefab, position, Quaternion.identity);
        cylinder.tag = "ScaffoldLine";
        cylinder.name = index.ToString();


        cylinder.transform.position = position;
        cylinder.transform.up = offset;
        cylinder.transform.localScale = scale;

        if (type == "constrained")
        {
            cylinder.transform.parent = detailScaffoldGroup.transform;
        }
        else
        {          
            cylinder.transform.parent = scaffoldGroup.transform;
        }

    }

    void ModifyCylinderStartAndEnd(GameObject gameObject, Vector3 start, Vector3 end)
    {
        var offset = end - start;
        var scale = new Vector3(width, offset.magnitude / 2.0f , width);
        var position = start + (offset / 2.0f);

        gameObject.transform.position = position;
        gameObject.transform.up = offset;
        gameObject.transform.localScale = scale;
    }


    private void DrawCurve(List<Vector3> points, float width, int index, string tag, GameObject parentGroup){

        GameObject gameObject = new GameObject();
        gameObject.name = index.ToString();

        gameObject.tag = tag;
        gameObject.transform.parent = parentGroup.transform;
        

        var lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));

        // Give strokes a different color
        if (tag == "Stroke")
        {
            lineRenderer.material.SetColor("_Color", new Color(0f, 0f, 1f, 0.7f));
        } 


        lineRenderer.startWidth = width;
        lineRenderer.endWidth = width;

        // random set to 4
        lineRenderer.numCapVertices = 4;
        lineRenderer.numCornerVertices = 4;
        

        lineRenderer.positionCount = points.Count;
        lineRenderer.SetPositions(points.ToArray());
    }

    private void ModifyCurve(GameObject gameObject, List<Vector3> points, int index){
        // reset the points

        // 1. get points from spline
        // 2. draw the spline points
        
        var lineRenderer = gameObject.GetComponent<LineRenderer>();  
        // lineRenderer.material = new Material(Shader.Find("Sprites/Default"));

        // random set to 4
        lineRenderer.numCapVertices = 4;
        lineRenderer.numCornerVertices = 4;
      
        lineRenderer.positionCount = points.Count;
        lineRenderer.SetPositions(points.ToArray());
    }


    // Find children object of a group
    public GameObject[] ChildrenOfGroup(GameObject parent)
    {

        List<GameObject> children = new List<GameObject>();

        Transform[] objChild = parent.transform.GetComponentsInChildren<Transform>();

        for (int i = 0; i < objChild.Length; i++)
        {
            if (objChild[i].gameObject != parent)
            {
                children.Add( objChild[i].gameObject );
                // Debug.Log(objChild[i].gameObject.name);            

            }
        }

        return children.ToArray();
    }



    void UpdateUIElements()
    {
        // update: update All the UI Elements
        // I might only need to update part of it
        // this might cause speed issues?

        scaffoldLines = GameObject.FindGameObjectsWithTag("ScaffoldLine");
        scaffoldPoints = GameObject.FindGameObjectsWithTag("ScaffoldPoint");
        curves = GameObject.FindGameObjectsWithTag("Curve");
        strokes = GameObject.FindGameObjectsWithTag("Stroke");



        foreach (var item in scaffoldLines)
        {
            var line_index = int.Parse(item.gameObject.name);
            Vector3[] endpoints = state.line_i_endpoints( line_index );
            ModifyCylinderStartAndEnd( item, endpoints[0], endpoints[1] );
        }

        foreach (var item in scaffoldPoints)
        {
            var point_index = int.Parse( item.gameObject.name );
            item.gameObject.transform.position = state.point_i_position( point_index );
        }

        foreach (var item in curves)
        {
            var curve_index = int.Parse( item.gameObject.name );
            List<Vector3> curve_points = state.curve_i_points( curve_index );
            ModifyCurve( item.gameObject, curve_points, curve_index);
        }

        foreach (var item in strokes)
        {
            var stroke_index = int.Parse( item.gameObject.name );
            List<Vector3> detail_i_points = state.stroke_i_points( stroke_index );
            ModifyCurve( item.gameObject, detail_i_points, stroke_index);
        }
    }

    // very naively add the newly drew stroke
    private void AddNewStroke( State state )
    {

        int sceneStrokesCount = strokeGroup.transform.childCount;

        int stateStrokesCount = state.strokes.Count;

        // should be 1 more
        while (stateStrokesCount > sceneStrokesCount)
        {
            // count from back
            int strokeIndex = stateStrokesCount - 1;
            stateStrokesCount -= 1;

            List<Vector3> stroke_points = state.stroke_i_points( strokeIndex );
            DrawCurve( stroke_points, curve_width, strokeIndex, "Stroke", strokeGroup);
            
        }


    }


    // 
    // A button pressed : Move -> Detail -> Curve -> Move -> ...
    // 
    public void ChangeCurrentState()
    {
        Debug.Log("ChangeCurrentState");

        
        if ( currentMode == Mode.Draw )
        {
            SetMoveMode();
        }
        else if (currentMode == Mode.Move)
        {
            SetDrawMode(); 
        }
        
        // I don't want to expose these 2 modes for testing
        // Because that will make the learning curve much more higher
        // 


        // else if (currentMode == Mode.Detail)
        // {
        //     SetCurveMode();
        // }
        // else
        // {
        //     SetDrawMode();
        // }

    }
    
    // Draw Mode : show scaffold, hide detailscaffoldGroup
    // 1. not allow scaffolds selection
    // 2. the user can draw
    private void SetDrawMode()
    {
        DisallowScaffoldsSelection();
        Debug.Log("current Mode:  draw");
    
        scaffoldGroup.SetActive( true );
        penBall.SetActive( true );
        detailScaffoldGroup.SetActive( false );

        current_mode_TMP.text = "Draw";


        currentMode = Mode.Draw;


    }

    // show all free and half_constrained lines
    // showing the scaffold 
    // 1. turn the detail mode off
    // 2. user can manipulate the free and half_constrained scaffold lines
    private void SetMoveMode()
    {
        current_mode_TMP.text = "Move";

        Debug.Log("current Mode:  Move");

        // added xue for to simply user study
        // hide the detail group
        detailScaffoldGroup.SetActive(false);


        AllowScaffoldsSelection();


        penBall.SetActive( false );



        currentMode = Mode.Move;

    }

    // once detail mode -> true, detail mode on
    // 1. all scaffoldlines cannot be selected 
    // 2. showing tick points 
    // 3. always show now in the detail mode
    private void SetDetailMode()
    {
        Debug.Log("current Mode:  Detail");

        // scaffoldGroup.SetActive( true );
        detailScaffoldGroup.SetActive( true );

        // 
        // update because detail group might change
        // and we want to update that after it showing
        // modify it before it showing will not work
        // when it is not active modify it will not work
        // 

        UpdateUIElements();


        foreach (var item in scaffoldLines)
        {
            item.GetComponent<ChangeColorWhenCollide>().canBeSelected = false;
        }


        current_mode_TMP.text = "Detail";

        currentMode = Mode.Detail;
    }


    // Curve Mode : Hide scaffoldGroup, detailScffoldGroup
    // maybe I can add extra function in this mode with other buttons
    private void SetCurveMode()
    {
        // scaffoldGroup.SetActive( false );
        // detailScaffoldGroup.SetActive( false );

        // current_mode_TMP.text = "Curve";

        // currentMode = Mode.Curve;

        // Debug.Log("current Mode:  curve");

        scaffoldGroup.SetActive(false);

        StartCoroutine("AutoHideShowScaffolds");

    }








    // 
    IEnumerator ShowText(TMP_Text tmp, string text, float delay)
    {

        tmp.text = text;

        yield return new WaitForSeconds( delay );

        tmp.text = "";
    }


    IEnumerator AutoHideShowScaffolds()
    {
        yield return (new WaitForSeconds(2));
        scaffoldGroup.SetActive( true );

    }

    /// 
    /// below are the scripts for the socket connection
    ///
    /// 

    WebSocket websocket;


    // Start is called before the first frame update
    async void Start()
    {
        // websocket = new WebSocket("ws://localhost:8999");
        websocket = new WebSocket("ws://xxx.xxx.xxx.xxx:8999");
        


        Debug.Log(websocket);

        websocket.OnOpen += () => {
            Debug.Log("Log Connection Open!");
            SendWebSocketMessage("init ");
            Debug.Log("send init message");
        };

        websocket.OnError += (e) => {
            Debug.Log("Log Error!" + e);
        };

        websocket.OnClose += (e) => {
            Debug.Log("Log Connection closed");
        };

        websocket.OnMessage += ReceiveMessage;
        
        await websocket.Connect();

    }

    async void SendWebSocketMessage(string text){
        if (websocket.State == WebSocketState.Open)
        {
            await websocket.SendText(text);
        }
    }



    private void SendMovedObjectsInformation()
    {
        
        if (currentMode == Mode.Move)
        {      
            ScaffoldLineJSON[] scaffoldsInstance = new ScaffoldLineJSON[selectedObjects.Count];

            for (int i = 0; i < selectedObjects.Count; i++)
            {
                var item = selectedObjects[i];

                // this might be wrong
                var line_index = int.Parse( item.gameObject.name );

                var line_start_point_index = state.lines[line_index].startIndex;
                var line_end_point_index = state.lines[line_index].endIndex;

                var start_position = state.points[line_start_point_index];
                var end_position = state.points[line_end_point_index];

                scaffoldsInstance[i] = new ScaffoldLineJSON(line_index, start_position.ToVector3(), end_position.ToVector3());
                // scaffoldsInstance[i] = new ScaffoldLineJSON(line_index, cylinder_endpoints[0], cylinder_endpoints[1]);
            }

            string scaffoldsToJson = JsonHelper.ToJson(scaffoldsInstance, true);
            // Debug.Log( scaffoldsToJson );
            SendWebSocketMessage( "move-line " + scaffoldsToJson );
            
        } 
        else
        {

            // Debug.Log("in detail mode selected objects " + selectedObjects.Count);

            
            ScaffoldPointJSON[] tickPointsInstance = new ScaffoldPointJSON[selectedObjects.Count];

            for (int i = 0; i < selectedObjects.Count; i++)
            {
                var item = selectedObjects[i];
                var tick_index = int.Parse( item.gameObject.name );
                float t = state.points[tick_index].x;
                
                tickPointsInstance[i] = new ScaffoldPointJSON(tick_index, t);
            }
            string detailScaffoldsToJSON = JsonHelper.ToJson(tickPointsInstance, true);
            SendWebSocketMessage("move-detail " + detailScaffoldsToJSON);
        }

    }

    private void ReceiveMessage(byte[] bytes)
    {
        var message = System.Text.Encoding.UTF8.GetString(bytes);    
        // Debug.Log("message: " + message);

        OnReceiveMessageEvent( message );
    }



    private void OnReceiveMessageEvent(string message){
        
        // Debug.Log( "connection receive message" + e.message );

        string[] subs = message.Split(' ');
        Debug.Assert( subs.Length > 0);
        string command = subs[0];
        
        if ( command == "init" )
        {
            var parameter = message.Substring(5);

            state = stateParser.parse( parameter );

            DrawState( state ); 

            SetCurrentMode();         
        }
        // add detail stroke
        else if ( command == "detail_stroke")
        {
            var parameter = message.Substring(14);

            state = stateParser.parse( parameter );

            DestroyImmediate( rawStrokeObject );
            
            AddNewStroke( state );            
        }

        else if ( command == "move_line")
        {
            var parameter = message.Substring(10); // move_line + space
            
            state = stateParser.parse( parameter );

            UpdateUIElements();   
            AllowScaffoldsSelection();         
        }
        else if ( command == "move_detail")
        {
            var parameter = message.Substring(12);

            state = stateParser.parse( parameter );

            UpdateUIElements();
            AllowScaffoldsSelection();         

        }
        else if (command == "undo")
        {
            StartCoroutine(ShowText(undo_redo_TMP, "Undo", delay));

            var parameter = message.Substring(5);

            state = stateParser.parse( parameter );
            
            DrawState( state ); 
            
            SetCurrentMode();
        }
        else if( command == "redo")
        {
            StartCoroutine(ShowText(undo_redo_TMP, "Redo", delay));
            var parameter = message.Substring(5);

            state = stateParser.parse( parameter );
            DrawState( state );

            SetCurrentMode();
        }

        Optimize_in_process_TMP.SetActive( false );
      
        Debug.Log("elapsed time: " + (Time.time - opt_start_time));
        // Debug.Log("Opt finished");

    }



    public void SendUndoMessage()
    {
        Debug.Log("undo ");
        SendWebSocketMessage("undo ");
    }

    public void SendRedoMessage()
    {
        SendWebSocketMessage("redo ");
    }

    public void SendExportMessage()
    {
        SendWebSocketMessage("export ");
        // Debug.Log(" export button clicked");
        
    }

    private async void OnApplicationQuit() {
        await websocket.Close();
    }

    // To send stroke to backend
    public class Data {
        public Vector3 scale;
        public Vector3[] points;
    }


    // remove all children of a gameobject
    // https://stackoverflow.com/questions/46358717/how-to-loop-through-and-destroy-all-children-of-a-game-object-in-unity

    private void RemoveAllChildrenOfGroup(GameObject parentObj)
    {
        var transform = parentObj.transform;
        while (transform.childCount > 0) {
            DestroyImmediate(transform.GetChild(0).gameObject);
        }
    }

}
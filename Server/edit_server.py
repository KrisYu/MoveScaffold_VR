import asyncio
import websockets
import socket
import json
import sys

import pathlib
import os
from datetime import datetime
from time import strftime

import sketch_modify
from copy import deepcopy

import fit_detail_stroke

def export( state_sequence, basename ):
    '''
    Given:
        state_sequence: All history states.
        basename: filename

    Saves the given sequence of states to the file `~/Desktop/VR_MoveScaffold_output/filename.json`
    '''
    
    ## 1. Create the output location if not exist
    ## 2. Save the state_sequence to file 

    # https://stackoverflow.com/questions/34275782/how-to-get-desktop-location
    desktop = pathlib.Path.home() / 'Desktop'
    # create a folder to store output
    output_dir = desktop / 'VR_MoveScaffold_output'  
    
    if not output_dir.exists():
        output_dir.mkdir()

    # save json file
    json_file = os.path.join( output_dir, basename + ".json")
    with open(json_file, "w") as f:
        json.dump( state_sequence, f )

    print( "Saved:", json_file )

def get_ip():
    s = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    s.settimeout(0)
    try:
        # doesn't even have to be reachable
        s.connect(('10.255.255.255', 1))
        IP = s.getsockname()[0]
    except Exception:
        IP = '127.0.0.1'
    finally:
        s.close()
    return IP


def run( load_file  ):

    ## 1. load file contents, when init received -> send to front end
    ## 2. whenever move-line, send the data to optimize and send back 
    ## 3. auto saves, with the 

    state = json.load(open(load_file)) # dict
    print(state.keys())

    if 'strokes' not in state:
        state['strokes'] = []
    if 'stroke_points' not in state:
        state['stroke_points'] = []
    print(state.keys())


    async def move_server(websocket, path):
        
        undo_buffer = [ deepcopy( state ) ]
        redo_buffer = []
        
        # I can even have all undo and redo history in here
        all_history_states = [ deepcopy( state ) ]


        def save_state_for_undo():
            all_history_states.append( deepcopy(state) )
            undo_buffer.append( deepcopy( state ) )
            del redo_buffer[:]

        def undo():
            # https://stackoverflow.com/questions/1281184/why-cant-i-set-a-global-variable-in-python
            nonlocal state
            if len(undo_buffer) > 1:
                # pop last saved state
                redo_buffer.append( undo_buffer.pop() )
                state = deepcopy( undo_buffer[-1] )
                    
        def redo():
            nonlocal state
            if len(redo_buffer) >= 1:
                state = deepcopy( redo_buffer[-1] )
                undo_buffer.append( redo_buffer.pop() )


        async for message in websocket:
            # print("server received: " + message)
            
            parsed = message.split( " ", 1 )
            command = parsed[0]

            parameters = None if len( parsed ) == 1 else parsed[1]
                
            if command == "init":
                await websocket.send( "init " + json.dumps( state )  )
            elif command == "detail-stroke":
                input_data = json.loads( parameters )
                fit_detail_stroke.stroke_data_to_weights( input_data, state )
                save_state_for_undo()
                await websocket.send("detail_stroke " + json.dumps( state )) 
            elif command == "move-line":
                input_data = json.loads( parameters )
#               import time
#               start = time.time()
                sketch_modify.move_line(input_data, state)
#               end = time.time()
#               print('elapsed', end  - start )
                save_state_for_undo()
                print('move-line ')
                # print(state)
                await websocket.send("move_line " + json.dumps( state ) ) 
            elif command == "move-detail":
                input_data = json.loads( parameters )
                # print('parameters', parameters)
                # print('input_data', input_data)
                print('move-detail ')
                sketch_modify.move_detail( input_data, state )
                save_state_for_undo()
                await websocket.send("move_detail " + json.dumps( state ) )
            elif command == "undo":
                undo()
                await websocket.send("undo " + json.dumps(state ) )
            elif command == "redo":
                redo()
                await websocket.send("redo " + json.dumps( state ) )

            export( all_history_states,  pathlib.Path(load_file).stem  + basename )


    start_server = websockets.serve(move_server, None, 8999)


    asyncio.get_event_loop().run_until_complete( start_server )
    asyncio.get_event_loop().run_forever()




url = get_ip()
print('Serving at http://{}:8999'.format(url))

# create with the name when load the server
now = datetime.today()
basename = now.strftime("_%Y_%m_%d_%H_%M_%S") + ( "_%d_%s" % ( now.microsecond / 1000, strftime( "%Z" ) ) )



load_file = 'Data/A1_cube_move.json'
if len(sys.argv) == 2:
    load_file = sys.argv[1]
run( load_file )
# # modify to load other files
# jsonText = json.load(open('jsonFile02.json'))
# print(type(jsonText))
# state = json.dumps( jsonText )
# print(type(state))
# print("IP address : " , get_ip())
# start_server = websockets.serve(echo, None, 8999)
# asyncio.get_event_loop().run_until_complete(start_server)
# asyncio.get_event_loop().run_forever()
# MoveScaffold

This is the code repository associated with the paper [A Scaffold-Based Tool for Product Design Variations in Virtual Reality](https://cragl.cs.gmu.edu/movescaffold/) by Xue Yu, Stephen DiVerdi and Yotam Gingold from CHI 2025.


## Set up

### Server Side

Build the enviroment and run :

```
conda env create -f environment.yml
conda activate opt_env
python edit_server.py (Data/*.json)
```

Runing the server will also print your local IP address.


### Client Side 

1. You need to go to `InitSketch.cs` line 1414 to change the websocket to your computer's IP address.
2. Then build the apk and install on Meta Quest, once it connects the server successfully, you'll see the loaded model and you can manipulate it.


## How to Run

check [How to Run](instruction/how_to.png).


## Output and Supplementary

Please check the [project page](https://cragl.cs.gmu.edu/movescaffold/) for detail.
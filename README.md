<img src="https://github.com/ArielOliveira/DungeonGenerator/blob/master/Assets/demo/DungeonWide.png" width="1258" height="300">

# DungeonGenerator
A Dungeon Generator made in Unity3D

## Contents

- [About](#about)
- [Features](#features)
- [Demo](#demo)

## About
- A C# implementation of Dr. Peter Henningsen's Dungeon Maker algorithm, which had further contribution from Aaron Dalton. The link
to the source forge project http://dungeonmaker.sourceforge.net/. 
- Basically, the algorithm makes use of AI to procedurally generate rooms and connects them with tunnels\corridors. There are 3 basic
structures and for each structure a type of builder: Tunnelers build tunnels, wall crawlers build walls (which can turn into mazes)
and Roomies build rooms. 
- The builders are like creatures living in a given map and they have a limited life, for every turn they will do their stuff and grow older, when
they get to their max age they are eliminated and there is a chance of them spawning babies (except for the roomies which are spawned by tunnelers
and will have only one run) in different (or the same) direction and thats one of the main ideas on how this algorithm works. 
You can see all the information on how it works, including the original source code, at the sourceforge project linked above.

## Features
- 3D Visualization of the algorithm's output
- Abstraction of the structures enabling further implementation for decoration assets
          
## Demo
- Video Clip: https://drive.google.com/file/d/1dS5h15slvBa3YsIhAAFWiGA4ESOwJ0F5/view?usp=sharing


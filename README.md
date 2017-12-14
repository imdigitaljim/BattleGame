## Project Objectives:
Develop a peer-to-peer game with Unity's game engine and have a procedural map generation mechanism as part of the gameplay. 

## Usage:

### Dependencies
This project requires Unity https://unity3d.com/get-unity/download

## Project Description

BattleGame is a peer-to-peer video game that applies similar rules to BattleShip. The start of the game requires each user to share connection information (IP Address and Port) to connect a peer-to-peer topology. (Note: default ports's are provided but would need port forwarding.) Additionally the appropriate size game must be selected by each user. 

The gameplay begins once each peer that is being expected appears. At this point, each player will place their n units. Once each unit places their units, the game signals ready for all. Then in order of player number, each player takes a turn. Their turn consists of clicking on any of their opponents maps for where they wish to guess. A marker will appear for the attacker and attackee showing where this interaction occurred. Other players do not see this information other than an attack was complete, consider it a fog of war. This is to prevent an advantage for another player. Once a unit is discovered from the click, it will be shown for all. The total count of remaining is updated. Once a user's total count becomes 0 they lose. Once all players have a count at 0 except a single player, that player is considered victorious. Each player will remain live in game until victory is declared. Any disconnects currently will interrupt the game.


## Gameplay TODO
- Different unit types and sizes for different terrains, currently one square unit with no different of placement
- Bounding box for placing units within a map, can currently edge off a little.
- Apply textures and animations
- Configurable gameplay such as: more water vs land, how many units to place, map sizes
## Framework Future TODO
- BattleGame does not do any NAT-PMP or UPnP so manual port forwarding would be necessary. 
- More seamless user connections, dynamically preferred instead of choosing player size.
- Make a matchmaking server kit such as an easy Python connect and aggregate
- Continuity of gameplay among disconnects, especially after losing.

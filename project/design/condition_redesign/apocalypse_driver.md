# Apocalypse Driver

We're building a web page to simulate the rules of a new game.

The game is called Apocalypse Driver.

The player is driving across a post-apocalyptic wasteland.

Driving costs gas.
Time costs food.

Each destination has a chance of having gas and or food.

The car has two types of problems:
- Minor, maintenance-oriented problems
- Major, critical problems

Minor problems can be ignored. Or you can spend time/food to repair them.
Minor problems have no impact. However, they make it more likely to get a major problem.

Major problems must be fixed by getting a certain repair part.
Major problems are on a timer. If they are not fixed in a certain amount of time you lose.

Each turn, the player reviews a set of possible destinations.
Each destination has a likelihood of having gas, food, or a type of repair part.
Each destination requires a different amount of gas to reach.
The player may also choose to spend the turn repairing a minor problem, but that costs food.

If the player runs out of gas or food, they lose.
If the player does not address a major problem in some number of turns, they lose.

## Balance Levers:
- Starting food, gas
- Base likelihood of a major problem (%)
- Increase chance of a major problem for each minor problem (%)
- Number of turns in which a major problem must be solved
- Possible range of gas, food, and parts at destinations
- Mutators to above range based on distance (gas required)

The web page should have the ability to modify any of the balance levers.
It should also allow the player to restart the game.
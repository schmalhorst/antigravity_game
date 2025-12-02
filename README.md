# AntigravityMoon 🌙

A survival and exploration game set on a mysterious alien moon. Battle hostile creatures, build structures, manage resources, and explore an infinite procedurally-generated world.

## About This Project

This game was created entirely using **Antigravity**, Google's advanced agentic AI coding assistant. The development process showcases the power of AI-assisted game development, with my kids providing creative input on assets, strategy, and design decisions throughout the project.

## Features

- **Infinite Procedural World**: Explore an endless, randomly generated lunar landscape with varied terrain including dust, rocks, and craters
- **Dynamic Minimap**: Track your exploration with a fog-of-war minimap that reveals the world as you discover it
- **Survival Mechanics**: Manage hunger, oxygen, and health to stay alive
- **Building System**: Construct greenhouses, workbenches, and other structures to survive
- **Alien Combat**: Defend yourself against the Metro Alien enemy with visual laser combat
- **Resource Management**: Harvest rocks, crystals, and grow corn to sustain yourself
- **Inventory System**: Upgradeable inventory with contextual item actions
- **Spaceship Base**: Return to your landing site to refill oxygen supplies

## Controls

- **WASD**: Move your astronaut
- **Mouse**: Aim and interact with objects
- **Left Click**: Harvest resources, attack enemies, interact with structures
- **I**: Open/Close Inventory
- **B**: Open Build Menu
- **M**: Toggle Minimap Size (Small/Large)
- **Right Click**: Cancel placement or open context menus

## Building & Crafting

- **Greenhouse** (2 Rocks): Grows corn for food
- **Workbench** (Free): Upgrade your inventory capacity with crystals

## How to Run

### Prerequisites
- [.NET 6.0 or later](https://dotnet.microsoft.com/download)
- MonoGame Framework

### Running the Game
```bash
cd AntigravityMoon
dotnet run
```

## Game Mechanics

### Survival
- **Hunger**: Depletes while moving. Eat corn to restore.
- **Oxygen**: Constantly depletes. Return to your spaceship to refill (costs 2 Rocks).
- **Health**: Take damage from alien attacks.

### Exploration
- Walk in any direction to generate new terrain
- The world is infinite and deterministic (same coordinates always generate the same terrain)
- Dark crater tiles are impassable barriers

### Combat
- Metro Aliens spawn after 30 seconds
- Click on aliens to shoot them with your laser
- Aliens explode when defeated, leaving behind valuable resources

## Credits

**Development**: Created with Antigravity AI
**Creative Direction**: John Schmalhorst & Kids
**Assets & Design Input**: Schmalhorst Family
**Framework**: MonoGame

## License

This project is a personal/educational project created with AI assistance.

---

*Built with ❤️ and AI by the Schmalhorst family*

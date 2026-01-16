Toy Box Factory - Automated Assembly with UR Robot
This repository contains the implementation of a database driven automated assembly system developed for the Industrial Programming three week project.
The project demonstrates how a C# desktop application, SQLite databases, and a Universal Robots (UR) industrial robot can be used to form a simplified but realistic automated production system aligned with Industry 4.0 and Industry 5.0 principles.

Project Overview
The objective of the project is to automate the assembly of toy boxes by digitally controlling production orders and translating them into physical robot actions.
The system allows an operator to:
- Create production orders via a graphical user interface
- Store and track orders persistently in a database
- Execute assembly operations using a UR robot
- Maintain traceability between digital orders and physical production

The solution focuses on clarity, traceability, safety, and extensibility, rather than full industrial scale automation.

System Architecture
The system is structured into clearly separated layers to ensure modularity and maintainability:
- GUI for the Operator Station
- Using Avalonia template desktop application
- Application & Domain Logic

Coordinates order handling, inventory updates, and robot execution.

Data Layer
- SQLite databases accessed via Entity Framework Core (EF Core).
- Robot Integration Layer
- URScript programs generated and sent to the robot over TCP/IP.

Production Flow
- Operator creates a production order in the GUI
- Order is stored in a SQLite database
- Application retrieves the next queued order
- Order data is translated into predefined robot motion sequences
- The UR robot executes the assembly process
- Order state and inventory quantities are updated in the database
- GUI reflects the updated system state

This architecture ensures that production state is persistent, traceable, and restart safe.

Robot Integration
- Communication with the robot is handled via TCP/IP
- Robot programs are generated dynamically in URScript
- Motion logic is centralized in a dedicated RobotPositions module
- Supports both URSim simulation and real robot execution
- Sensor input is used to ensure safe sequencing and placement

Separating robot motion definitions from UI and database logic allows for easier calibration, testing, and future optimization.

Database Design
The database acts as the single source of truth for production state.
SQLite is used as the database engine.
Entity Framework Core (EF Core) is used for ORM access.

Two databases are included:
inventory.sqlite for production data (inventory, orders), you can use check DB to locate file in the project files.
auth.sqlite for authentication and user management

Core Concepts
Inventory and Items
Orders and OrderLines
OrderBook with queued and processed orders
Persistent state across application restarts
A controlled seed and reset mechanism is implemented to support testing and demonstrations without deleting database files.

Security
The system includes a basic but realistic security implementation:
Login system with salted and hashed passwords
Role based access control (Admin / Operator)
Administrative actions restricted to admin users
Separate authentication database
This reflects fundamental operational technology security principles taught in the course.

Testing & Demonstration
Tested using URSim and physical robot hardware
Robot sequences validated through iterative calibration
Database reset functionality enables repeatable demonstrations
Demonstration video is provided separately (see report/presentation)

Future Extensions (Out of Scope)
Conveyor belt integration
Vision system (camera-based part detection)
Additional robots
Fully automated scheduling
Advanced analytics and optimization

AI Usage Disclosure
This project was developed with limited assistance from a generative AI tool.

Tool used:
ChatGPT (OpenAI, 2025)
How AI was used:
As a feedback and code assistance tool during development
- To generate an initial structural skeleton for the Avalonia GUI and MVVM architecture based on our own assignments, activity diagrams, lecture notes, and project planning
- To help identify and resolve build errors and clarify XAML code.
- For sparring related to course material and written explanations
- To suggest improvements to comments, structure, and simplification of ViewModel logic
- To assist with drafting the README file and an initial class diagram structure, which were subsequently rewritten and adapted into my own wording
- To suggest code for optimization of gui, databases and robot positions.

Author responsibility:
All code has been written, reviewed, adapted, and understood by the group.
We have verified the logic manually, modified the structure where necessary, and added own comments to demonstrate understanding of the curriculum.
We take full responsibility for the final implementation, system design, documentation, and submitted solution.

Authors
Lars Bach Sørensen - s235648
Lasse Manicus - s235655
Marius Millington - s235659
Developed as part of Industrial Programming at DTU.

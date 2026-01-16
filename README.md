Toy Box Factory – Automated Assembly with UR Robot
This repository contains a database driven automated assembly system developed for the Industrial Programming three week project.
The project demonstrates how a C# desktop application, SQLite databases, and a Universal Robots (UR) industrial robot can be integrated to form a simplified but realistic automated production system.

Project Overview
The objective of the project is to automate the assembly of toy boxes by digitally controlling production orders and translating them into physical robot actions.
The system allows an operator to:
- Create production orders via a graphical user interface
- Store and track orders persistently in a database
- Execute assembly operations using a UR robot
- Maintain traceability between digital orders and physical production
The solution prioritizes clarity, traceability, safety, and extensibility, rather than full industrial-scale automation.

Getting Started / How to Run the Project
- This section describes how to run and test the project after cloning the repository.

Prerequisites
- NET SDK
- Visual Studio or JetBrains Rider
- Universal Robots URSim or access to a physical UR robot
- Ethernet connection (for real robot communication)

Running the Application
- Open the solution file InventorySystem2.sln
- Build the solution
- Run the application

On first startup, the application will:
- Create the SQLite database files if they do not exist
- Seed initial inventory and order book data
- Create a default admin user if the authentication database is empty

Login
- Default admin credentials on first run:
- Username: admin
- Password: admin
- After login, admin users can create additional users through the GUI

Database
The system uses two SQLite databases:
- inventory.sqlite – production data (inventory, orders, order state)
- auth.sqlite – authentication and user management
Notes:
- Database files are stored alongside the application output
- The “Check DB” button in the GUI verifies database connectivity and file location
- The “Reset DB” button restores the database to its initial seeded state, enabling repeatable demonstrations without deleting database files

Robot Connection
- Default robot IP is set to localhost for URSim

For a real robot:
- Enter the robot’s IP address in the GUI
- Ensure the robot is in Remote Control mode
- Ensure the PC and robot are on the same network

Robot control details:
- Robot programs are generated dynamically in URScript
- Programs are sent to the robot controller over a TCP/IP network connection
- The same URScript is used for both URSim and real robot execution

Testing Without a Robot
- The system can be fully tested using URSim
- Order creation, database updates, and robot command generation work without physical hardware
- This allows safe testing and demonstration.

System Architecture (Overview)
- The system is structured into clearly separated layers to ensure modularity and maintainability:

GUI / Operator Station
- Avalonia desktop application using the MVVM pattern
- Provides operator control for order handling, database actions, and robot connection

Application & Domain Logic
- Coordinates order processing, inventory updates, and robot execution
- Bridges database state and robot actions

Data Layer
- SQLite databases accessed via Entity Framework Core (EF Core)
- Acts as the single source of truth for production state

Robot Integration Layer
- URScript programs generated in C#
- Sent to the robot via TCP/IP
- Robot motion logic centralized in a dedicated RobotPositions module

Production Flow
- Operator creates a production order in the GUI
- Order is stored persistently in the SQLite database
- Application retrieves the next queued order
- Order data is translated into predefined robot motion sequences
- The UR robot executes the assembly process
- Order state and inventory quantities are updated in the database
- GUI reflects the updated system state
This architecture ensures that production state is persistent, traceable, and restart-safe.

Testing & Demonstration
- Tested using URSim and physical robot hardware
- Robot sequences validated through iterative calibration
- Database reset functionality enables repeatable demonstrations
Demonstration video is provided separately (see report/presentation)

AI Usage Disclosure
This project was developed with limited assistance from a generative AI tool.
Source:
ChatGPT (OpenAI, 2025)

How AI was used:
- As a feedback and code assistance tool during development
- To generate an initial structural skeleton for the Avalonia GUI and MVVM architecture based on our own assignments, activity diagrams, lecture notes, and project planning
- To help identify and resolve build errors and clarify XAML code
- For sparring related to course material and written explanations
- To suggest improvements to comments, structure, and simplification of ViewModel logic
- To assist with drafting the README file and an initial class diagram structure, which were subsequently rewritten and adapted into our own wording
- To suggest optimizations related to GUI structure, database handling, and robot position logic

Author responsibility:
All code has been written, reviewed, adapted, and understood by the group.
We have verified the logic manually, modified the structure where necessary, and added our own comments to demonstrate understanding of the curriculum.
We take full responsibility for the final implementation, system design, documentation, and submitted solution.
Authors
Lars Bach Sørensen – s235648
Lasse Manicus – s235655
Marius Millington – s235659
Developed as part of Industrial Programming at DTU.

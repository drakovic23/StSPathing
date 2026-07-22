using System.Diagnostics;
using System.Text;

namespace DirectedAcrylicGraph;

class Program
{
    public class Map
    {
        public class Room
        {
            public int FloorNumber { get; private set; }
            public int SlotNumber { get; private set; }
            readonly List<Room> _superiorRooms;
            public IReadOnlyList<Room> SuperiorRooms => _superiorRooms;
            public char CharacterId; // This is for debugging purposes for this example
            internal Room(int floorNumber, int slotNumber)
            {
                FloorNumber = floorNumber;
                SlotNumber = slotNumber;
                _superiorRooms = new();
            }
            public bool AddConnection(Room room)
            {
                if (room.FloorNumber != this.FloorNumber + 1)
                    return false;

                if (_superiorRooms.Contains(room))
                    return false;
                
                _superiorRooms.Add(room);
                return true;
            }
        }
        
        readonly Dictionary<int, List<Room>> _map = new();
        HashSet<Room> _rooms = new();
        public int MaxSlots { get; private set; } = 4;
        int _seed = 0;
        public Map(int seed)
        {
            _seed = seed;
        }
        public void AddRoomToMap(Room room)
        {
            if (_rooms.Contains(room)) // Duplicate map
                return;
            
            if(!_map.ContainsKey(room.FloorNumber))
                _map.Add(room.FloorNumber, new List<Room>());
            
            _map[room.FloorNumber].Add(room);
            
            _rooms.Add(room);
        }

        public List<Room> GetRooms(int floorNumber)
        {
            if (_map.TryGetValue(floorNumber, out List<Room> rooms))
            {
                return rooms;
            }
            
            return Array.Empty<Room>().ToList();
        }

        public bool Connect(Room from, Room to, bool checkForCrossing = true)
        {
            if(_rooms.Contains(from) && _rooms.Contains(to))
            {
                List<Room> roomsOnFloorFrom = _map[from.FloorNumber];

                if (checkForCrossing)
                {
                    foreach (var neighborRooms in roomsOnFloorFrom)
                    {
                        if (neighborRooms == from) // Don't check the room against itself
                            continue;

                        foreach (Room connectedRoom in neighborRooms.SuperiorRooms)
                        {
                            bool crossesLeft = to.SlotNumber < connectedRoom.SlotNumber && from.SlotNumber > neighborRooms.SlotNumber;
                            bool crossesRight = to.SlotNumber > connectedRoom.SlotNumber && from.SlotNumber < neighborRooms.SlotNumber;
                        
                            if(crossesLeft || crossesRight)
                            {
                                return false;
                            }
                        }
                    }
                }

                if (from.AddConnection(to))
                {
                    // Console.WriteLine($"Connected: {from.CharacterId} to {to.CharacterId}");
                    return true;
                }
            }

            return false;
        }
        
        // Generate the floors and rooms
        public void Generate(int maxFloors, int maxRoomsPerFloor)
        {
            char characterId = '@';
            Random random = new(_seed);
            
            for (int i = 0; i < maxFloors; i++)
            {
                int floorNumber = i + 1;

                List<int> availableSlots = new();
                for (int s = 0; s < MaxSlots; s++)
                    availableSlots.Add(s);
                
                for (int p = 0; p < maxRoomsPerFloor; p++)
                {
                    int availableSlotIdx = random.Next(0, availableSlots.Count);
                    Room newRoom = new Room(floorNumber, availableSlots[availableSlotIdx]);
                    availableSlots.RemoveAt(availableSlotIdx);
                    
                    newRoom.CharacterId = ++characterId;
                    
                    
                    // Console.WriteLine($"Adding new room to map with floor number: {floorNumber}, ID: {newRoom.CharacterId}");
                    AddRoomToMap(newRoom);
                    
                    if (floorNumber == maxFloors && p == 0) // Boss floor
                        break;
                }
            }

            BuildConnections(maxFloors);
        }
        
        // Build the connections
        public void BuildConnections(int maxFloors)
        {
            Random random = new(_seed);
            for (int floorNumber = 2; floorNumber < maxFloors; floorNumber++)
            {
                int lowerFloorIdx = floorNumber - 1;
                foreach (var upperFloorRoom in _map[floorNumber])
                {


                    if (floorNumber > 2) // Floors greater than 2 can have multiple connections
                    {
                        int left = upperFloorRoom.SlotNumber - 1;
                        int center = upperFloorRoom.SlotNumber;
                        int right = upperFloorRoom.SlotNumber + 1;

                        if (left < 0)
                            left = 0;
                        if (right > MaxSlots)
                            right = MaxSlots - 1;

                        int maxConnections = 2;
                        int connectionsMade = 0;
                        foreach (var lowerFloorRoom in _map[lowerFloorIdx])
                        {
                            if (lowerFloorRoom.SlotNumber == left || lowerFloorRoom.SlotNumber == center || lowerFloorRoom.SlotNumber == right)
                            {
                                bool hasConnected = Connect(lowerFloorRoom, upperFloorRoom);
                                if(hasConnected)
                                    connectionsMade += 1;
                                if (connectionsMade >= maxConnections)
                                    break;
                            }
                        }
                        
                        if(connectionsMade == 0)
                            throw new InvalidOperationException($"Failed to find connection for {upperFloorRoom.CharacterId}");
                    }
                    else
                    {
                        // Pick a floor under our current floor and connect
                        int randomLowerRoomIndex = random.Next(0, _map[lowerFloorIdx].Count);
                        Connect(_map[lowerFloorIdx][randomLowerRoomIndex], upperFloorRoom);
                    }
                }
            }
            
            // Connect our boss room to all lower rooms
            Room bossRoom = _map[maxFloors][0];
            foreach (var lowerFloorRoom in _map[maxFloors - 1])
            {
                if (!lowerFloorRoom.SuperiorRooms.Contains(bossRoom))
                {
                    if(!Connect(lowerFloorRoom, bossRoom, false))
                        throw new InvalidOperationException($"Failed to connect boss room to {lowerFloorRoom.CharacterId}");
                }
            }
            
            // Do a pass from the first floor to the 2nd last floor before the boss and repair any paths
            for (int floorNumber = 1; floorNumber < maxFloors; floorNumber++)
            {
                foreach (var currentRoom in _map[floorNumber])
                {
                    if (currentRoom.SuperiorRooms.Count == 0)
                    {
                        // Console.WriteLine($"Detected dead end on {currentRoom.CharacterId}, repairing..");

                        bool connectionFound = false;
                        foreach (var topRoom in _map[floorNumber + 1])
                        {
                            int left = currentRoom.SlotNumber - 1;
                            int center = currentRoom.SlotNumber;
                            int right = currentRoom.SlotNumber + 1;

                            if (left < 0)
                                left = 0;
                            if (right > MaxSlots)
                                right = MaxSlots - 1;
                            
                            // Note we only allow one connection here, something to consider
                            if (topRoom.SlotNumber == left || topRoom.SlotNumber == center || topRoom.SlotNumber == right)
                            {
                                bool hasConnected = Connect(currentRoom, topRoom);
                                
                                if (hasConnected)
                                {
                                    connectionFound = true;
                                    break;
                                }
                            }
                        }
                        
                        if(!connectionFound)
                            throw new InvalidOperationException($"Failed to repair dead end on {currentRoom.CharacterId} on floor {currentRoom.FloorNumber} with slot {currentRoom.SlotNumber}");
                    }
                }
            }
        }

        public bool Validate(int maxFloors)
        {
            HashSet<Room> visited = new();
            Queue<Room> roomQueue = new();

            foreach (var room in _map[1])
            {
                roomQueue.Enqueue(room);
                visited.Add(room);
            }

            while (roomQueue.Count > 0)
            {
                var current = roomQueue.Dequeue();
                int index = 0;
                
                foreach (var room in current.SuperiorRooms)
                {
                    if (!visited.Contains(room))
                    {
                        roomQueue.Enqueue(room);
                        visited.Add(room);
                    }
                }
            }
            
            if(visited.Count == _rooms.Count)
            {
                bool hasDeadEnds = CheckForDeadEnds(maxFloors);

                if (!hasDeadEnds)
                {
                    ConsoleColor originalColor = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Validation successful");
                    Console.ForegroundColor = originalColor;
                }
                return !hasDeadEnds;
            }
            else
            {
                ConsoleColor originalColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error during validation, visited.Count != _rooms.Count");
                Console.ForegroundColor = originalColor;
                return false;
            }
        }
        
        public void PrintAllRooms()
        {
            Console.WriteLine(" ");

            foreach (var kvp in _map)
            {
                foreach (var room in kvp.Value)
                {
                    Console.WriteLine($"Floor number: {room.FloorNumber}, ID: {room.CharacterId}");
                }
            }
        }

        public bool CheckForDeadEnds(int maxFloors)
        {
            ConsoleColor originalColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            foreach (var room in _rooms)
                if (room.FloorNumber < maxFloors && room.SuperiorRooms.Count == 0)
                {
                    Console.WriteLine($"DEAD END: {room.CharacterId} on floor {room.FloorNumber}");
                    return true;
                }
            
            Console.ForegroundColor = originalColor;

            return false;
        }

        public Room[] BuildRow(int floor)
        {
            Room[] row = new Room[MaxSlots];
            foreach (Room room in GetRooms(floor))
            {
                row[room.SlotNumber] = room;
            }

            return row;
        }

        const int GRID_CELL_WIDTH = 4;
        int Center(int slot) => slot * GRID_CELL_WIDTH + 1;

        string RoomRow(Room[] row)
        {
            char[] line = new string(' ', MaxSlots * GRID_CELL_WIDTH).ToCharArray();
            for (int slot = 0; slot < MaxSlots; slot++)
                line[Center(slot)] = row[slot]?.CharacterId ?? '.';
            return new string(line);
        }
        
        string ConnectorRow(Room[] lowerRow)
        {
            char[] line = new string(' ', MaxSlots * GRID_CELL_WIDTH).ToCharArray();

            foreach (Room from in lowerRow)
            {
                if (from == null) continue;

                foreach (Room to in from.SuperiorRooms)
                {
                    int delta = to.SlotNumber - from.SlotNumber;
                    int x = Center(from.SlotNumber) + delta * (GRID_CELL_WIDTH / 2);
                    char glyph = delta == 0 ? '|' : delta > 0 ? '/' : '\\';

                    line[x] = (line[x] == ' ' || line[x] == glyph) ? glyph : 'X';
                }
            }

            return new string(line);
        }
        public void PrintGrid(int maxFloors)
        {
            for (int floor = maxFloors; floor >= 1; floor--)
            {
                Console.WriteLine($"{floor,3} {RoomRow(BuildRow(floor))}");

                if (floor > 1)
                    Console.WriteLine($"    {ConnectorRow(BuildRow(floor - 1))}");
            }
        }
    }

    static void Main(string[] args)
    {
        int maxFloors = 10;
        int maxRoomsPerFloor = 2;
        for (int i = 0; i < 10000; i++)
        {
            Map map = new(i);
            try
            {
                map.Generate(maxFloors, maxRoomsPerFloor);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Caught Exception: {e.Message}");
                map.PrintGrid(maxFloors);
                return;
            }
            map.Validate(20);
        }
    }
}

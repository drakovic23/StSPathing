using System.Diagnostics;

namespace DirectedAcrylicGraph;

class Program
{
    public class Map
    {
        public class Room
        {
            public int FloorNumber { get; private set; }
            readonly List<Room> _superiorRooms;
            public IReadOnlyList<Room> SuperiorRooms => _superiorRooms;
            public char CharacterId; // This is temporary
            internal Room(int floorNumber)
            {
                FloorNumber = floorNumber;
                _superiorRooms = new();
            }
            public bool AddConnection(Room room)
            {
                if (room.FloorNumber != this.FloorNumber + 1)
                    return false;
            
                _superiorRooms.Add(room);
                return true;
            }
        }
        
        readonly Dictionary<int, List<Room>> _map = new();
        HashSet<Room> _rooms = new();
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

        public bool Connect(Room from, Room to)
        {
            if(_rooms.Contains(from) && _rooms.Contains(to))
            {
                if (from.AddConnection(to))
                {
                    // Console.WriteLine($"Connected: {from.CharacterId} to {to.CharacterId}");
                    return true;
                }
            }

            return false;
        }

        public void Generate(int maxFloors, int maxRoomsPerFloor)
        {
            char characterId = '@';
            
            for (int i = 0; i < maxFloors; i++)
            {
                int floorNumber = i + 1;
                
                for (int p = 0; p < maxRoomsPerFloor; p++)
                {
                    Room newRoom = new Room(floorNumber);
                    newRoom.CharacterId = ++characterId;

                    
                    
                    // Console.WriteLine($"Adding new room to map with floor number: {floorNumber}, ID: {newRoom.CharacterId}");
                    AddRoomToMap(newRoom);
                    
                    if (floorNumber == maxFloors && p == 0) // Boss floor
                        break;
                }
            }

            BuildConnections(maxFloors);
        }

        public void BuildConnections(int maxFloors)
        {
            Random random = new();
            for (int floorNumber = 2; floorNumber < maxFloors; floorNumber++)
            {
                int lowerFloor = floorNumber - 1;
                foreach (var upperFloorRoom in _map[floorNumber])
                {
                    // Pick a floor under our current floor and connect
                    int randomLowerRoomIndex = random.Next(0, _map[lowerFloor].Count);
                    Connect(_map[lowerFloor][randomLowerRoomIndex], upperFloorRoom);
                }
            }
            
            // Connect our boss room to all lower rooms
            Room bossRoom = _map[maxFloors][0];
            foreach (var lowerFloorRoom in _map[maxFloors - 1])
            {
                if (!lowerFloorRoom.SuperiorRooms.Contains(bossRoom))
                {
                    if(!Connect(lowerFloorRoom, bossRoom))
                        throw new InvalidOperationException($"Failed to repair dead end at {lowerFloorRoom.CharacterId}");
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

                        int roomIndex = random.Next(0, _map[floorNumber + 1].Count);
                        Room connectingRoom = _map[floorNumber + 1][roomIndex];
                        if (!Connect(currentRoom, connectingRoom))
                        {
                            throw new InvalidOperationException($"Failed to repair dead end at {currentRoom.CharacterId}");
                        }
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
    }

    static void Main(string[] args)
    {
        for (int i = 0; i < 10000; i++)
        {
            Map map = new();
            map.Generate(20, 2);
            map.Validate(20);
        }
    }
}

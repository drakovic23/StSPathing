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
        Random _random;
        public int MaxSlots { get; private set; } // Max slots per row
        int _seed = 0;
        public Map(int seed, int maxSlots)
        {
            _seed = seed;
            MaxSlots = maxSlots;
            _random =  new(_seed);
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

        void Connect(Room from, Room to)
        {
            if (to.FloorNumber != from.FloorNumber + 1)
                throw new ArgumentException(
                $"Cannot connect floor {from.FloorNumber} to floor {to.FloorNumber}");

            from.AddConnection(to);   // Silently fail if the edge already exists
        }
        
        // Generate the floors and rooms
        public void Generate(int pathCount, int maxFloors)
        {
            for (int i = 0; i < pathCount; i++) // Start at bottom floor level
            {
                int randomSlot = _random.Next(0, MaxSlots); // Pick a random slot
                Room current = GetOrCreateRoom(1, randomSlot); // Start at first floor for each walk, work our way upwards

                for (int floor = 1; floor < maxFloors - 1; floor++) // Climb up until the last boss
                {
                    Room nextRoom = GetOrCreateRoom(floor + 1, PickNextSlot(current));
                    Connect(current, nextRoom);
                    current = nextRoom;
                }
                
                Connect(current, GetOrCreateRoom(maxFloors, MaxSlots / 2));
            }
            
            
        }

        public Room GetOrCreateRoom(int floorNumber, int slot)
        {
            if (_map.TryGetValue(floorNumber, out List<Room> rooms))
            {
                foreach (var room in rooms)
                {
                    if (room.FloorNumber == floorNumber && room.SlotNumber == slot) // The room exists
                        return room;
                }
            }

            Room newRoom = new Room(floorNumber, slot);
            newRoom.CharacterId = 'o';
            
            if(!_map.ContainsKey(newRoom.FloorNumber))
                _map.Add(floorNumber, new List<Room>());
            
            _map[floorNumber].Add(newRoom);
            _rooms.Add(newRoom);
            
            return newRoom;
        }

        public int PickNextSlot(Room from) // Picks a valid slot from the given room
        {
            List<int> availableSlots = new();

            for (int slot = from.SlotNumber - 1; slot <= from.SlotNumber + 1; slot++)
            {
                if (slot < 0 || slot >= MaxSlots)
                    continue;

                if (!IsValidSlot(from, slot))
                    continue;
                
                availableSlots.Add(slot);
            }

            if (availableSlots.Count == 0)
                throw new InvalidOperationException("Unable to find available slots");
            
            return availableSlots[_random.Next(0, availableSlots.Count)];
        }

        bool IsValidSlot(Room from, int toSlot)
        {
            foreach (Room neighbor in GetRooms(from.FloorNumber))
            {
                if (neighbor == from)
                    continue;

                foreach (Room connectedRoom in neighbor.SuperiorRooms)
                {
                    int neighborSlot = neighbor.SlotNumber;
                    int connectedSlot = connectedRoom.SlotNumber;
                    
                    bool crossesLeft = toSlot < connectedSlot && from.SlotNumber > neighborSlot;
                    bool crossesRight = toSlot > connectedSlot && from.SlotNumber < neighborSlot;

                    if (crossesLeft || crossesRight)
                        return false;
                }
            }

            return true;
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

            foreach (var room in _rooms)
                if (room.FloorNumber < maxFloors && room.SuperiorRooms.Count == 0)
                {
                    ConsoleColor originalColor = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"DEAD END: {room.CharacterId} on floor {room.FloorNumber}");
                    Console.ForegroundColor = originalColor;
                    return true;
                }
            

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
                    Console.WriteLine($"    {ConnectorRow(BuildRow(floor - 1))}"); // The padding here is required, do not remove
            }
        }
    }

    static void Main(string[] args)
    {
        int maxFloors = 10;
        int maxSlots = 7;
        for (int i = 0; i < 10000; i++)
        {
            Map map = new(0, maxSlots);
            try
            {
                map.Generate(6, maxFloors);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Caught Exception: {e.ToString()}");
                map.PrintGrid(maxFloors);
                return;
            }
            // map.PrintGrid(maxFloors);
            map.Validate(maxFloors);
        }
    }
}

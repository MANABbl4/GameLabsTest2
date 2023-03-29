using System;
using System.Collections.Generic;

namespace Assets.Scripts
{
    public static class GraphSearch
    {
        public static List<int> FindPath(Dictionary<int, HashSet<int>> graph, int start, int end)
        {
            var previous = new Dictionary<int, int>();
            var queue = new Queue<int>();

            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                int current = queue.Dequeue();

                if (current == end)
                {
                    return BuildPath(previous, start, end);
                }

                foreach (int neighbor in graph[current])
                {
                    if (!previous.ContainsKey(neighbor))
                    {
                        queue.Enqueue(neighbor);
                        previous[neighbor] = current;
                    }
                }
            }

            return null;
        }

        private static List<int> BuildPath(Dictionary<int, int> previous, int start, int end)
        {
            var path = new List<int>();
            int current = end;

            while (current != start)
            {
                path.Insert(0, current);
                current = previous[current];
            }

            path.Insert(0, start);
            return path;
        }
    }
}

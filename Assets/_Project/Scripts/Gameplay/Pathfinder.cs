using System.Collections.Generic;
using UnityEngine;
using PSA.Core;
using PSA.Gameplay.Grid;

namespace PSA.Gameplay
{
    public class Pathfinder : MonoBehaviour, ISystem
    {
        private readonly Vector2Int[] _directions =
        {
            Vector2Int.up,
            Vector2Int.right,
            Vector2Int.down,
            Vector2Int.left
        };

        private void OnDestroy()
        {
            SystemLocator.Deregister<Pathfinder>();
        }

        public void Initialize()
        {
            SystemLocator.Register(this);
        }

        /// <summary>
        /// BFS 
        /// </summary>
        public List<Cell> FindPath(Vector2Int startCoord, List<Cell> targetCells)
        {
            GridManager gridManager = SystemLocator.Get<GridManager>();
            if (!gridManager) return null;

            Cell startCell = gridManager.GetCellAt(startCoord);
            if (!startCell || targetCells == null || targetCells.Count == 0) return null;

            Queue<Cell> queue = new Queue<Cell>();
            HashSet<Cell> visited = new HashSet<Cell>();

            Dictionary<Cell, Cell> parentMap = new Dictionary<Cell, Cell>();

            queue.Enqueue(startCell);
            visited.Add(startCell);

            Cell reachedEndNode = null;

            while (queue.Count > 0)
            {
                Cell currentCell = queue.Dequeue();

                if (targetCells.Contains(currentCell))
                {
                    reachedEndNode = currentCell;
                    break;
                }

                foreach (Vector2Int dir in _directions)
                {
                    Vector2Int neighborCoord = currentCell.Coordinate + dir;
                    Cell neighborCell = gridManager.GetCellAt(neighborCoord);

                    if (neighborCell && !visited.Contains(neighborCell))
                    {
                        bool isWalkable = !neighborCell.IsOccupied || targetCells.Contains(neighborCell);

                        if (isWalkable)
                        {
                            visited.Add(neighborCell);
                            parentMap[neighborCell] = currentCell;
                            queue.Enqueue(neighborCell);
                        }
                    }
                }
            }

            return !reachedEndNode ? null : RetracePath(startCell, reachedEndNode, parentMap);
        }

        private List<Cell> RetracePath(Cell startNode, Cell endNode, Dictionary<Cell, Cell> parentMap)
        {
            List<Cell> path = new List<Cell>();
            Cell currentNode = endNode;

            while (currentNode != startNode)
            {
                path.Add(currentNode);
                currentNode = parentMap[currentNode];
            }

            path.Reverse();
            return path;
        }
    }
}
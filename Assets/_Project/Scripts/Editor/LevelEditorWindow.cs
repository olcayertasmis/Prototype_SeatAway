using PSA.Gameplay.Data;
using UnityEditor;
using UnityEngine;

namespace PSA.Editor
{
    public class LevelEditorWindow : EditorWindow
    {
        private enum PaintMode
        {
            GridCell,
            Seat
        }

        private enum QueueDirectionPreset
        {
            Back,
            Forward,
            Right,
            Left,
        }

        private LevelData _currentLevelData;
        private SerializedObject _serializedLevelData;

        // Base Grid
        private SerializedProperty _levelNameProp;
        private SerializedProperty _timeLimitProp;

        // Grid Props
        private SerializedProperty _gridWidthProp;
        private SerializedProperty _gridHeightProp;
        private SerializedProperty _gridCellsProp;

        private SerializedProperty _passengerQueueProp;
        private SerializedProperty _seatsProp;

        // Entrance & Queue
        private SerializedProperty _entranceCoordinateProp;
        private SerializedProperty _queueDirectionProp;

        private PaintMode _currentMode = PaintMode.GridCell;

        private CellType _selectedCellType = CellType.Empty;
        private ElementColor _selectedSeatColor = ElementColor.Blue;

        private int _seatWidth = 1;
        private int _seatHeight = 1;
        private bool _isSeatMovable = true;

        private Vector2 _scrollPosition;

        [MenuItem("Tools/Level Editor")]
        public static void ShowWindow()
        {
            GetWindow<LevelEditorWindow>("Level Editor");
        }

        private void OnEnable()
        {
            if (_currentLevelData)
            {
                InitializeSerializedObject();
            }
        }

        private void OnGUI()
        {
            GUILayout.Label("Level Data Editor", EditorStyles.boldLabel);

            LevelData newData = (LevelData)EditorGUILayout.ObjectField("Level Data", _currentLevelData, typeof(LevelData), false);

            if (newData != _currentLevelData)
            {
                _currentLevelData = newData;
                InitializeSerializedObject();
            }

            if (!_currentLevelData)
            {
                EditorGUILayout.HelpBox("Please assign or create a Level Data to start editing.", MessageType.Info);
                return;
            }

            if (_serializedLevelData == null)
            {
                InitializeSerializedObject();
            }

            if (_serializedLevelData != null)
            {
                _serializedLevelData.Update();

                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

                DrawLevelSettings();
                EditorGUILayout.Space();

                DrawQueueSettings();
                EditorGUILayout.Space();

                DrawToolbar();
                EditorGUILayout.Space();

                DrawVisualGrid();

                EditorGUILayout.EndScrollView();

                _serializedLevelData.ApplyModifiedProperties();
            }
        }

        private void InitializeSerializedObject()
        {
            if (!_currentLevelData) return;

            _serializedLevelData = new SerializedObject(_currentLevelData);
            _levelNameProp = _serializedLevelData.FindProperty("levelName");
            _timeLimitProp = _serializedLevelData.FindProperty("timeLimit");
            _gridWidthProp = _serializedLevelData.FindProperty("gridWidth");
            _gridHeightProp = _serializedLevelData.FindProperty("gridHeight");
            _gridCellsProp = _serializedLevelData.FindProperty("gridCells");
            _passengerQueueProp = _serializedLevelData.FindProperty("passengerQueue");
            _seatsProp = _serializedLevelData.FindProperty("seats");
            _entranceCoordinateProp = _serializedLevelData.FindProperty("entranceCoordinate");
            _queueDirectionProp = _serializedLevelData.FindProperty("queueDirection");

            _serializedLevelData.Update();
            ValidateGridSize();
            _serializedLevelData.ApplyModifiedProperties();
        }

        private void DrawLevelSettings()
        {
            EditorGUILayout.PropertyField(_levelNameProp);
            EditorGUILayout.PropertyField(_timeLimitProp);
            EditorGUILayout.PropertyField(_passengerQueueProp, new GUIContent("Passenger Spawn Order"));

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(_gridWidthProp);
            EditorGUILayout.PropertyField(_gridHeightProp);

            if (EditorGUI.EndChangeCheck())
            {
                _gridWidthProp.intValue = Mathf.Max(1, _gridWidthProp.intValue);
                _gridHeightProp.intValue = Mathf.Max(1, _gridHeightProp.intValue);
                ValidateGridSize();
            }
        }

        private void DrawQueueSettings()
        {
            GUILayout.Label("Queue Settings", EditorStyles.boldLabel);

            GUI.enabled = false;
            EditorGUILayout.PropertyField(_entranceCoordinateProp, new GUIContent("Selected Entrance"));
            GUI.enabled = true;

            Vector3 currentDir = _queueDirectionProp.vector3Value;
            QueueDirectionPreset currentPreset;

            if (currentDir == Vector3.left) currentPreset = QueueDirectionPreset.Left;
            else if (currentDir == Vector3.forward) currentPreset = QueueDirectionPreset.Forward;
            else if (currentDir == Vector3.back) currentPreset = QueueDirectionPreset.Back;
            else currentPreset = QueueDirectionPreset.Right;

            EditorGUI.BeginChangeCheck();
            QueueDirectionPreset newPreset = (QueueDirectionPreset)EditorGUILayout.EnumPopup("Queue Growth Direction", currentPreset);

            if (EditorGUI.EndChangeCheck())
            {
                _queueDirectionProp.vector3Value = newPreset switch
                {
                    QueueDirectionPreset.Right => Vector3.right,
                    QueueDirectionPreset.Left => Vector3.left,
                    QueueDirectionPreset.Forward => Vector3.forward,
                    QueueDirectionPreset.Back => Vector3.back,
                    _ => Vector3.right
                };
            }
        }

        private void DrawToolbar()
        {
            GUILayout.Label("Paint Tools", EditorStyles.boldLabel);

            GUILayout.BeginHorizontal();
            if (GUILayout.Toggle(_currentMode == PaintMode.GridCell, "Edit Ground (Cell)", "Button")) _currentMode = PaintMode.GridCell;
            if (GUILayout.Toggle(_currentMode == PaintMode.Seat, "Edit Seats (Objects)", "Button")) _currentMode = PaintMode.Seat;
            GUILayout.EndHorizontal();

            EditorGUILayout.Space();

            if (_currentMode == PaintMode.GridCell)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Toggle(_selectedCellType == CellType.Empty, "Eraser", "Button")) _selectedCellType = CellType.Empty;
                if (GUILayout.Toggle(_selectedCellType == CellType.Obstacle, "Obstacle", "Button")) _selectedCellType = CellType.Obstacle;
                //if (GUILayout.Toggle(_selectedCellType == CellType.Entrance, "Entrance", "Button")) _selectedCellType = CellType.Entrance;
                GUILayout.EndHorizontal();
            }
            else
            {
                float originalLabelWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = 200f;

                _isSeatMovable = EditorGUILayout.Toggle("Is Movable (Uncheck for Static)", _isSeatMovable);

                EditorGUIUtility.labelWidth = originalLabelWidth;

                if (_isSeatMovable)
                {
                    _selectedSeatColor = (ElementColor)EditorGUILayout.EnumPopup("Seat Color", _selectedSeatColor);
                }
                else
                {
                    GUI.enabled = false;
                    EditorGUILayout.EnumPopup("Seat Color", ElementColor.Blue);
                    GUI.enabled = true;
                }

                GUILayout.BeginHorizontal();
                _seatWidth = EditorGUILayout.IntSlider("Width", _seatWidth, 1, 3);
                _seatHeight = EditorGUILayout.IntSlider("Height", _seatHeight, 1, 3);
                GUILayout.EndHorizontal();

                EditorGUILayout.HelpBox("Clicking a cell will place the BOTTOM-LEFT corner of the Seat. Click again to remove it.", MessageType.Info);
            }
        }

        private void DrawVisualGrid()
        {
            GUILayout.Label("Grid Layout", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("The column on the RIGHT represents Entrance options. It automatically scales with grid height.", MessageType.Info);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.BeginVertical();

            int width = _gridWidthProp.intValue;
            int height = _gridHeightProp.intValue;

            for (int y = height - 1; y >= 0; y--)
            {
                GUILayout.BeginHorizontal();

                for (int x = 0; x < width; x++)
                {
                    DrawCell(x, y, width);
                }

                GUILayout.Space(20);

                DrawEntranceCell(width, y);

                GUILayout.EndHorizontal();
            }

            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private void DrawCell(int x, int y, int width)
        {
            int index = y * width + x;
            if (index >= _gridCellsProp.arraySize) return;

            SerializedProperty cellProp = _gridCellsProp.GetArrayElementAtIndex(index);
            SerializedProperty typeProp = cellProp.FindPropertyRelative("cellType");

            CellType currentType = (CellType)typeProp.enumValueIndex;

            bool hasSeat = false;
            string seatLabel = "";
            Color buttonColor = GUI.color;

            for (int i = 0; i < _seatsProp.arraySize; i++)
            {
                SerializedProperty seatProp = _seatsProp.GetArrayElementAtIndex(i);
                Vector2Int root = seatProp.FindPropertyRelative("rootCoordinate").vector2IntValue;
                int sW = seatProp.FindPropertyRelative("width").intValue;
                int sH = seatProp.FindPropertyRelative("height").intValue;

                if (x >= root.x && x < root.x + sW && y >= root.y && y < root.y + sH)
                {
                    hasSeat = true;
                    ElementColor sColor = (ElementColor)seatProp.FindPropertyRelative("color").enumValueIndex;
                    bool isM = seatProp.FindPropertyRelative("isMovable").boolValue;

                    if (isM)
                    {
                        seatLabel = $"[S]\n{sColor}";
                        GUI.color = sColor switch
                        {
                            ElementColor.Blue => Color.cyan,
                            ElementColor.Green => Color.green,
                            ElementColor.Yellow => Color.yellow,
                            ElementColor.Red => Color.red,
                            _ => Color.black // error oldugu anlasilsin
                        };
                    }
                    else
                    {
                        seatLabel = "[Static]";
                        GUI.color = Color.white;
                    }

                    break;
                }
            }

            string finalLabel = hasSeat
                ? seatLabel
                : currentType switch
                {
                    CellType.Obstacle => "X\nObs",
                    //CellType.Entrance => "IN",
                    _ => ""
                };

            GUIStyle cellStyle = new GUIStyle(GUI.skin.button) { fixedWidth = 50, fixedHeight = 50, fontSize = 10 };

            if (GUILayout.Button(finalLabel, cellStyle))
            {
                if (_currentMode == PaintMode.GridCell)
                {
                    if (!hasSeat) typeProp.enumValueIndex = (int)_selectedCellType;
                }
                else
                {
                    if (hasSeat) RemoveSeatAt(x, y);
                    else PlaceSeat(x, y);
                }
            }

            GUI.color = buttonColor;
        }

        private void DrawEntranceCell(int gridWidth, int y)
        {
            Vector2Int currentEntrance = _entranceCoordinateProp.vector2IntValue;
            bool isSelectedEntrance = (currentEntrance.x == gridWidth && currentEntrance.y == y);

            Color oldColor = GUI.color;

            GUI.color = isSelectedEntrance ? Color.green : new Color(0.3f, 0.3f, 0.3f);

            GUIStyle cellStyle = new GUIStyle(GUI.skin.button) { fixedWidth = 50, fixedHeight = 50, fontSize = 10 };
            string label = isSelectedEntrance ? "ENTRANCE" : "[ ]";

            if (GUILayout.Button(label, cellStyle))
            {
                _entranceCoordinateProp.vector2IntValue = new Vector2Int(gridWidth, y);

                _queueDirectionProp.vector3Value = Vector3.right;
            }

            GUI.color = oldColor;
        }

        private void PlaceSeat(int x, int y)
        {
            if (x + _seatWidth > _gridWidthProp.intValue || y + _seatHeight > _gridHeightProp.intValue) return;

            for (int i = 0; i < _seatWidth; i++)
            {
                for (int j = 0; j < _seatHeight; j++)
                {
                    int checkX = x + i;
                    int checkY = y + j;
                    int cellIndex = checkY * _gridWidthProp.intValue + checkX;

                    if (cellIndex < _gridCellsProp.arraySize)
                    {
                        SerializedProperty cellProp = _gridCellsProp.GetArrayElementAtIndex(cellIndex);
                        int cellType = cellProp.FindPropertyRelative("cellType").enumValueIndex;

                        if (cellType != (int)CellType.Empty) return;
                    }
                }
            }

            for (int i = 0; i < _seatsProp.arraySize; i++)
            {
                SerializedProperty seatProp = _seatsProp.GetArrayElementAtIndex(i);
                Vector2Int root = seatProp.FindPropertyRelative("rootCoordinate").vector2IntValue;
                int sW = seatProp.FindPropertyRelative("width").intValue;
                int sH = seatProp.FindPropertyRelative("height").intValue;

                bool overlapX = x < root.x + sW && x + _seatWidth > root.x;
                bool overlapY = y < root.y + sH && y + _seatHeight > root.y;

                if (overlapX && overlapY) return;
            }

            _seatsProp.arraySize++;
            SerializedProperty newSeat = _seatsProp.GetArrayElementAtIndex(_seatsProp.arraySize - 1);
            newSeat.FindPropertyRelative("rootCoordinate").vector2IntValue = new Vector2Int(x, y);
            newSeat.FindPropertyRelative("width").intValue = _seatWidth;
            newSeat.FindPropertyRelative("height").intValue = _seatHeight;
            newSeat.FindPropertyRelative("color").enumValueIndex = (int)_selectedSeatColor;
            newSeat.FindPropertyRelative("isMovable").boolValue = _isSeatMovable;

            //newSeat.FindPropertyRelative("color").enumValueIndex = _isSeatMovable ? (int)_selectedSeatColor : (int)ElementColor.Transparent;
        }

        private void RemoveSeatAt(int x, int y)
        {
            for (int i = 0; i < _seatsProp.arraySize; i++)
            {
                SerializedProperty seatProp = _seatsProp.GetArrayElementAtIndex(i);
                Vector2Int root = seatProp.FindPropertyRelative("rootCoordinate").vector2IntValue;
                int sW = seatProp.FindPropertyRelative("width").intValue;
                int sH = seatProp.FindPropertyRelative("height").intValue;

                if (x >= root.x && x < root.x + sW && y >= root.y && y < root.y + sH)
                {
                    _seatsProp.DeleteArrayElementAtIndex(i);
                    break;
                }
            }
        }

        private void ValidateGridSize()
        {
            int requiredSize = _gridWidthProp.intValue * _gridHeightProp.intValue;
            while (_gridCellsProp.arraySize < requiredSize)
            {
                _gridCellsProp.arraySize++;
                _gridCellsProp.GetArrayElementAtIndex(_gridCellsProp.arraySize - 1).FindPropertyRelative("cellType").enumValueIndex = (int)CellType.Empty;
            }

            if (_gridCellsProp.arraySize > requiredSize) _gridCellsProp.arraySize = requiredSize;

            int width = _gridWidthProp.intValue;
            for (int i = 0; i < _gridCellsProp.arraySize; i++)
            {
                _gridCellsProp.GetArrayElementAtIndex(i).FindPropertyRelative("coordinates").vector2IntValue = new Vector2Int(i % width, i / width);
            }

            Vector2Int ent = _entranceCoordinateProp.vector2IntValue;
            ent.x = width;
            ent.y = Mathf.Clamp(ent.y, 0, _gridHeightProp.intValue - 1);
            _entranceCoordinateProp.vector2IntValue = ent;
        }
    }
}
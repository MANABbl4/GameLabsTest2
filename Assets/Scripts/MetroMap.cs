using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

namespace Assets.Scripts
{
    [Serializable]
    public enum LineType : int
    {
        Red = 0,
        Green = 1,
        Blue = 2,
        Black = 3,
    }

    [Serializable]
    public class Station
    {
        [SerializeField]
        public string Name;

        [SerializeField]
        public Vector2 Position;
    }

    [Serializable]
    public class Link
    {
        [SerializeField]
        public string Station1;

        [SerializeField]
        public string Station2;

        [SerializeField]
        public LineType Line;
    }

    public class MetroMap : MonoBehaviour
    {
        [SerializeField]
        private List<Station> _stations;
        [SerializeField]
        private List<Link> _links;
        [SerializeField]
        private GameObject _stationPrefab;
        [SerializeField]
        private GameObject _linkPrefab;
        [SerializeField]
        private Text _pathText;
        [SerializeField]
        private Text _transfersText;

        private List<GameObject> _stationObjects = new List<GameObject>();
        private Dictionary<int, HashSet<int>> _metroGraph = new Dictionary<int, HashSet<int>>();
        private Dictionary<int, Dictionary<int, LineType>> _metroLinksGraph = new Dictionary<int, Dictionary<int, LineType>>();
        private Dictionary<string, int> _stationMap = new Dictionary<string, int>();
        private Dictionary<StationBehaviour, int> _stationBehaviourMap = new Dictionary<StationBehaviour, int>();
        private int _fromStation = -1;
        private int _toStation = -1;
        private int _selectedStation = -1;
        private StringBuilder _pathBuilder = new StringBuilder();

        private void Start()
        {
            for (int i = 0; i < _stations.Count; ++i)
            {
                var station = _stations[i];
                if (_stationMap.ContainsKey(station.Name))
                {
                    Debug.LogError($"Station {station.Name} already exists. Ignore it.");
                }

                _stationMap.Add(station.Name, i);

                var stationObject = GameObject.Instantiate(_stationPrefab);
                stationObject.transform.position = station.Position;
                stationObject.GetComponentInChildren<Canvas>().worldCamera = Camera.main;
                stationObject.GetComponentInChildren<Text>().text = station.Name;
                stationObject.name = station.Name;

                var behaviour = stationObject.GetComponent<StationBehaviour>();
                _stationBehaviourMap.Add(behaviour, i);
                behaviour.OnSelected += OnStationSelected;
                behaviour.OnUnselected += OnStationUnselected;

                _stationObjects.Add(stationObject);
            }

            foreach (Link link in _links)
            {
                var station1 = _stationMap.ContainsKey(link.Station1) ? _stations[_stationMap[link.Station1]] : null;
                var station2 = _stationMap.ContainsKey(link.Station2) ? _stations[_stationMap[link.Station2]] : null;

                if (station1 == null || station2 == null)
                {
                    Debug.LogError($"Invalid Link setup between stations {link.Station1} and {link.Station2}. It will be ignored.");

                    if (station1 == null)
                    {
                        Debug.LogError($"There is no station {link.Station1}");
                    }

                    if (station2 == null)
                    {
                        Debug.LogError($"There is no station {link.Station2}");
                    }
                    continue;
                }

                if (!_metroGraph.ContainsKey(_stationMap[link.Station1]))
                {
                    _metroGraph.Add(_stationMap[link.Station1], new HashSet<int>());
                    _metroLinksGraph.Add(_stationMap[link.Station1], new Dictionary<int, LineType>());
                }
                if (!_metroGraph.ContainsKey(_stationMap[link.Station2]))
                {
                    _metroGraph.Add(_stationMap[link.Station2], new HashSet<int>());
                    _metroLinksGraph.Add(_stationMap[link.Station2], new Dictionary<int, LineType>());
                }

                if (!_metroGraph[_stationMap[link.Station1]].Contains(_stationMap[link.Station2]))
                {
                    _metroGraph[_stationMap[link.Station1]].Add(_stationMap[link.Station2]);
                    _metroLinksGraph[_stationMap[link.Station1]].Add(_stationMap[link.Station2], link.Line);
                }
                if (!_metroGraph[_stationMap[link.Station2]].Contains(_stationMap[link.Station1]))
                {
                    _metroGraph[_stationMap[link.Station2]].Add(_stationMap[link.Station1]);
                    _metroLinksGraph[_stationMap[link.Station2]].Add(_stationMap[link.Station1], link.Line);
                }

                var linkObject = GameObject.Instantiate(_linkPrefab);
                linkObject.name = $"{station1.Name} {station2.Name} {link.Line}";
                var linkRenderer = linkObject.GetComponent<LineRenderer>();
                linkRenderer.SetPosition(0, station1.Position);
                linkRenderer.SetPosition(1, station2.Position);
                linkRenderer.startColor = GetLineColor(link.Line);
                linkRenderer.endColor = GetLineColor(link.Line);
            }
        }

        private void Update()
        {
            if (_selectedStation >= 0)
            {
                if (Input.GetMouseButtonUp((int)MouseButton.RightMouse))
                {
                    if (_selectedStation == _toStation)
                    {
                        _toStation = -1;
                    }
                    else if (_selectedStation == _fromStation)
                    {
                        if (_toStation >= 0)
                        {
                            _fromStation = _toStation;
                            _toStation = -1;
                        }
                        else
                        {
                            _fromStation = -1;
                        }
                    }
                }
                else if (Input.GetMouseButtonUp((int)MouseButton.LeftMouse))
                {
                    if (_fromStation >= 0 && _toStation >= 0)
                    {
                        if (_selectedStation != _fromStation)
                        {
                            SetStationUnchoosen(_fromStation);
                        }
                        if (_selectedStation != _toStation)
                        {
                            SetStationUnchoosen(_toStation);
                        }

                        _fromStation = -1;
                        _toStation = -1;
                    }

                    if (_fromStation < 0)
                    {
                        _fromStation = _selectedStation;

                        SetStationChoosen(_fromStation);
                    }
                    else if (_toStation < 0)
                    {
                        _toStation = _selectedStation;

                        SetStationChoosen(_toStation);

                        FindPath();
                    }
                }
            }
        }

        private Color GetLineColor(LineType lineType)
        {
            switch (lineType)
            {
                case LineType.Red: return Color.red;
                case LineType.Green: return Color.green;
                case LineType.Blue: return Color.blue;
                case LineType.Black: return Color.black;
                default: return Color.white;
            }
        }

        private void SetStationChoosen(int i)
        {
            var meshRenderer = _stationObjects[i].GetComponent<MeshRenderer>();
            meshRenderer.material.color = Color.red;
        }

        private void SetStationUnchoosen(int i)
        {
            var meshRenderer = _stationObjects[i].GetComponent<MeshRenderer>();
            meshRenderer.material.color = Color.white;
        }

        private void OnStationSelected(StationBehaviour station)
        {
            _selectedStation = _stationBehaviourMap[station];
        }

        private void OnStationUnselected(StationBehaviour station)
        {
            var unselectedStationIndex = _stationBehaviourMap[station];

            if (unselectedStationIndex == _fromStation || unselectedStationIndex == _toStation)
            {
                SetStationChoosen(unselectedStationIndex);
            }

            _selectedStation = -1;
        }

        private void FindPath()
        {
            var path = GraphSearch.FindPath(_metroGraph, _fromStation, _toStation);

            int transfersCount = 0;
            _pathBuilder.Clear();
            for (int i = 0; i < path.Count; ++i)
            {
                _pathBuilder.Append(_stations[path[i]].Name);
                if (i < path.Count - 1)
                {
                    _pathBuilder.Append("->");

                    if (i > 0)
                    {
                        if (_metroLinksGraph[path[i]][path[i-1]] != _metroLinksGraph[path[i+1]][path[i]])
                        {
                            ++transfersCount;
                        }
                    }
                }
            }

            _pathText.text = _pathBuilder.ToString();
            _transfersText.text = transfersCount.ToString();
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElementEngine.MachineLearning
{
    public class MarkovGenerator
    {
        private int _keySize { get; set; }
        private List<string> _data { get; set; }
        private Dictionary<string, Dictionary<string, int>> _table = new Dictionary<string, Dictionary<string, int>>();
        private List<string> _startingStates = new List<string>();
        private Random _rng;

        private List<string> _tempStringList = new List<string>();

        public MarkovGenerator(List<string> data, int keySize, Random rng = null)
        {
            _data = data;
            _keySize = keySize;
            _rng = rng ?? new Random();

            if (_keySize == 0)
                _keySize = 3;

            GenerateTable();
        }

        public string GetRandomString(int? maxSearchLength = null)
        {
            return GetRandomString(null, maxSearchLength);
        }

        public string GetRandomString(string startingValue, int? maxSearchLength = null)
        {
            if (string.IsNullOrEmpty(startingValue))
                startingValue = GetRandomStartingString();

            var newString = startingValue;
            var prevKey = startingValue;
            var searchLength = 0;

            while (true)
            {
                var nextKey = GetNextKey(prevKey);

                if (nextKey == null)
                    break;

                newString += nextKey[nextKey.Length - 1];

                searchLength += 1;

                if (maxSearchLength.HasValue && searchLength >= maxSearchLength.Value)
                    break;

                prevKey = nextKey;
            }

            return newString;
        } // GetRandomString

        private string GetRandomStartingString()
        {
            var index = _rng.Next(0, _startingStates.Count);
            return _startingStates[index];
        }

        private string GetNextKey(string key)
        {
            if (!_table.TryGetValue(key, out var keyData))
                return null;

            _tempStringList.Clear();

            foreach (var (str, count) in keyData)
            {
                for (var i = 0; i < count; i++)
                    _tempStringList.Add(str);
            }

            if (_tempStringList.Count == 0)
                return null;

            var nextKey = _tempStringList[_rng.Next(0, _tempStringList.Count)];
            return nextKey;

        } // GetNextKey

        private void GenerateTable()
        {
            _table.Clear();

            foreach (var wordData in _data)
            {
                var word = wordData.ToLower();

                if (word.Length <= _keySize)
                {
                    AddKey(word, "");
                    continue;
                }

                var prevKey = "";

                for (var i = 0; i < word.Length; i++)
                {
                    if (i + _keySize > word.Length)
                        break;

                    var key = word.Substring(i, _keySize);

                    if (prevKey.Length > 0)
                    {
                        AddKey(prevKey, key);
                    }
                    else
                    {
                        if (!_startingStates.Contains(key))
                            _startingStates.Add(key);
                    }

                    prevKey = key;
                }
            }

        } // GenerateTable

        private void AddKey(string key, string val)
        {
            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(val))
                return;

            if (!_table.TryGetValue(key, out var keyData))
            {
                keyData = new Dictionary<string, int>();
                _table.Add(key, keyData);
            }

            if (!keyData.TryGetValue(val, out var _))
                keyData.Add(val, 0);

            keyData[val] += 1;
        }

    } // MarkovGenerator
}

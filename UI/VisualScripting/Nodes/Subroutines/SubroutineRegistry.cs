using System;
using System.Collections.Generic;
using System.Linq;

namespace BasicToMips.UI.VisualScripting.Nodes.Subroutines
{
    /// <summary>
    /// Singleton registry that tracks all defined subroutines and functions
    /// Used for validation and dropdown population
    /// </summary>
    public class SubroutineRegistry
    {
        #region Singleton

        private static SubroutineRegistry? _instance;
        private static readonly object _lock = new object();

        public static SubroutineRegistry Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new SubroutineRegistry();
                        }
                    }
                }
                return _instance;
            }
        }

        private SubroutineRegistry()
        {
            _subroutines = new Dictionary<string, SubroutineInfo>();
            _functions = new Dictionary<string, FunctionInfo>();
        }

        #endregion

        #region Properties

        private Dictionary<string, SubroutineInfo> _subroutines;
        private Dictionary<string, FunctionInfo> _functions;

        #endregion

        #region Public Methods

        /// <summary>
        /// Get list of all defined subroutine names
        /// </summary>
        public List<string> GetDefinedSubroutines()
        {
            lock (_lock)
            {
                return _subroutines.Keys.OrderBy(k => k).ToList();
            }
        }

        /// <summary>
        /// Get list of all defined function names
        /// </summary>
        public List<string> GetDefinedFunctions()
        {
            lock (_lock)
            {
                return _functions.Keys.OrderBy(k => k).ToList();
            }
        }

        /// <summary>
        /// Validate a CALL statement
        /// </summary>
        /// <param name="name">Name of subroutine or function to call</param>
        /// <param name="isFunction">True if calling a function, false if calling a subroutine</param>
        /// <returns>True if the call is valid</returns>
        public bool ValidateCall(string name, bool isFunction)
        {
            lock (_lock)
            {
                if (isFunction)
                {
                    return _functions.ContainsKey(name);
                }
                else
                {
                    return _subroutines.ContainsKey(name);
                }
            }
        }

        /// <summary>
        /// Refresh the registry from the current graph
        /// Should be called when nodes are added, removed, or renamed
        /// </summary>
        /// <param name="nodes">All nodes in the current graph</param>
        public void RefreshRegistry(IEnumerable<NodeBase> nodes)
        {
            lock (_lock)
            {
                _subroutines.Clear();
                _functions.Clear();

                foreach (var node in nodes)
                {
                    if (node is SubDefinitionNode subDef)
                    {
                        if (!string.IsNullOrWhiteSpace(subDef.SubroutineName))
                        {
                            _subroutines[subDef.SubroutineName] = new SubroutineInfo
                            {
                                Name = subDef.SubroutineName,
                                NodeId = subDef.Id
                            };
                        }
                    }
                    else if (node is FunctionDefinitionNode funcDef)
                    {
                        if (!string.IsNullOrWhiteSpace(funcDef.FunctionName))
                        {
                            _functions[funcDef.FunctionName] = new FunctionInfo
                            {
                                Name = funcDef.FunctionName,
                                NodeId = funcDef.Id
                            };
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Register a subroutine definition
        /// </summary>
        public void RegisterSubroutine(string name, Guid nodeId)
        {
            lock (_lock)
            {
                _subroutines[name] = new SubroutineInfo
                {
                    Name = name,
                    NodeId = nodeId
                };
            }
        }

        /// <summary>
        /// Register a function definition
        /// </summary>
        public void RegisterFunction(string name, Guid nodeId)
        {
            lock (_lock)
            {
                _functions[name] = new FunctionInfo
                {
                    Name = name,
                    NodeId = nodeId
                };
            }
        }

        /// <summary>
        /// Unregister a subroutine
        /// </summary>
        public void UnregisterSubroutine(string name)
        {
            lock (_lock)
            {
                _subroutines.Remove(name);
            }
        }

        /// <summary>
        /// Unregister a function
        /// </summary>
        public void UnregisterFunction(string name)
        {
            lock (_lock)
            {
                _functions.Remove(name);
            }
        }

        /// <summary>
        /// Clear all registrations
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                _subroutines.Clear();
                _functions.Clear();
            }
        }

        /// <summary>
        /// Check if a subroutine name is already defined
        /// </summary>
        public bool IsSubroutineDefined(string name)
        {
            lock (_lock)
            {
                return _subroutines.ContainsKey(name);
            }
        }

        /// <summary>
        /// Check if a function name is already defined
        /// </summary>
        public bool IsFunctionDefined(string name)
        {
            lock (_lock)
            {
                return _functions.ContainsKey(name);
            }
        }

        /// <summary>
        /// Check if a name is used by either a subroutine or function
        /// </summary>
        public bool IsNameTaken(string name)
        {
            lock (_lock)
            {
                return _subroutines.ContainsKey(name) || _functions.ContainsKey(name);
            }
        }

        #endregion

        #region Inner Classes

        /// <summary>
        /// Information about a defined subroutine
        /// </summary>
        public class SubroutineInfo
        {
            public string Name { get; set; } = string.Empty;
            public Guid NodeId { get; set; }
        }

        /// <summary>
        /// Information about a defined function
        /// </summary>
        public class FunctionInfo
        {
            public string Name { get; set; } = string.Empty;
            public Guid NodeId { get; set; }
        }

        #endregion
    }
}

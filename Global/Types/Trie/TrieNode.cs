using System.Collections.Generic;

namespace SexyPackages
{
/// <summary> Represents a Trie Node </summary>

public class TrieNode
{
/// <summary> Node value </summary>

public char Value{ get; }

/// <summary> Sub-tries </summary>

public Dictionary<char, TrieNode> Children{ get; } = new();

/// <summary> Determines if node is a finalizer (ends with '\0') </summary>

public bool IsTerminal{ get; set; }

/// <summary> Raw Node Payload </summary>

public NativeBuffer Payload{ get; set; }

/// <summary> Sub-trie size </summary>

public int SubtreeSize{ get; set; }

/// <summary> Children Count </summary>

public int ChildCount => Children.Count;

/// <summary> Determines if this node has Children </summary>

public bool HasChildren => Children.Count > 0;

/// <summary> Children Values </summary>

public IEnumerable<TrieNode> ChildValues => Children.Values;

/// <summary> Payload Length in bytes </summary>

public ulong PayloadLength => Payload.Size;

// ctor

public TrieNode(char c)
{
Value = c;
}

// Add child

public void AddChild(char c, TrieNode child) => Children.Add(c, child);

// Try get child

public bool TryGetChild(char c, out TrieNode child) => Children.TryGetValue(c, out child);
}

}
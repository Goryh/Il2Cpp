using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Unity.IL2CPP.Common;

namespace Unity.IL2CPP.DataModel.Awesome.CFG;

public class Node
{
	private Node _parent;

	private int _index;

	private readonly NodeType _type;

	private readonly ReadOnlyCollection<Node> _children;

	private readonly InstructionBlock _block;

	private readonly ExceptionHandler _handler;

	public int Id { get; internal set; }

	public NodeType Type => _type;

	public InstructionBlock Block => _block;

	public ReadOnlyCollection<Node> Children => _children;

	public Node Parent => _parent;

	public bool IsInTryBlock
	{
		get
		{
			Node parent = this;
			while (parent != null && parent.Type != NodeType.Root)
			{
				parent = parent.Parent;
				if (parent.Type == NodeType.Try)
				{
					return true;
				}
				if (parent.Type == NodeType.Catch)
				{
					return false;
				}
				if (parent.Type == NodeType.Filter)
				{
					return false;
				}
				if (parent.Type == NodeType.Finally)
				{
					return false;
				}
				if (parent.Type == NodeType.Fault)
				{
					return false;
				}
			}
			return false;
		}
	}

	public bool IsInCatchBlock
	{
		get
		{
			Node parent = this;
			while (parent != null && parent.Type != NodeType.Root)
			{
				if (parent.Type == NodeType.Try)
				{
					return false;
				}
				if (parent.Type == NodeType.Catch)
				{
					return true;
				}
				if (parent.Type == NodeType.Filter)
				{
					return false;
				}
				if (parent.Type == NodeType.Finally)
				{
					return false;
				}
				if (parent.Type == NodeType.Fault)
				{
					return false;
				}
				parent = parent.Parent;
			}
			return false;
		}
	}

	public bool IsInFinallyOrFaultBlock
	{
		get
		{
			Node parent = this;
			while (parent != null && parent.Type != NodeType.Root)
			{
				parent = parent.Parent;
				if (parent.Type == NodeType.Try)
				{
					return false;
				}
				if (parent.Type == NodeType.Catch)
				{
					return false;
				}
				if (parent.Type == NodeType.Filter)
				{
					return false;
				}
				if (parent.Type == NodeType.Finally)
				{
					return true;
				}
				if (parent.Type == NodeType.Fault)
				{
					return true;
				}
			}
			return false;
		}
	}

	private Node NextSibling
	{
		get
		{
			if (Parent == null)
			{
				return null;
			}
			if (_index == Parent.Children.Count - 1)
			{
				return null;
			}
			return Parent.Children[_index + 1];
		}
	}

	public ExceptionHandler Handler => _handler;

	private Node Root
	{
		get
		{
			Node parent = this;
			while (parent != null && parent.Type != NodeType.Root)
			{
				parent = parent.Parent;
			}
			return parent;
		}
	}

	public int Depth
	{
		get
		{
			int depth = 0;
			for (Node parent = _parent; parent != null; parent = parent.Parent)
			{
				depth++;
			}
			return depth;
		}
	}

	public int TryBlockDepth
	{
		get
		{
			int depth = 0;
			for (Node current = this; current != null; current = current.Parent)
			{
				if (current.Type == NodeType.Try)
				{
					depth++;
				}
			}
			return depth;
		}
	}

	public Node[] CatchNodes
	{
		get
		{
			if (_type != 0)
			{
				throw new NotSupportedException("Cannot find the related finally handler for a non-try block");
			}
			List<Node> handlers = new List<Node>();
			Node current = NextSibling;
			while (current != null && current.Type == NodeType.Catch)
			{
				handlers.Add(current);
				current = current.NextSibling;
			}
			return handlers.ToArray();
		}
	}

	public Node[] FilterNodes
	{
		get
		{
			if (_type != 0)
			{
				throw new NotSupportedException("Cannot find the related finally handler for a non-try block");
			}
			List<Node> handlers = new List<Node>();
			Node current = NextSibling;
			while (current != null && current.Type == NodeType.Filter)
			{
				handlers.Add(current);
				current = current.NextSibling;
			}
			return handlers.ToArray();
		}
	}

	public Node FinallyNode
	{
		get
		{
			if (_type != 0)
			{
				throw new NotSupportedException("Cannot find the related finally handler for a non-try block");
			}
			Node nextNode = NextSibling;
			if (nextNode == null || nextNode.Type != NodeType.Finally)
			{
				return null;
			}
			return nextNode;
		}
	}

	public Node FaultNode
	{
		get
		{
			if (_type != 0)
			{
				throw new NotSupportedException("Cannot find the related fault handler for a non-try block");
			}
			Node nextNode = NextSibling;
			if (nextNode == null || nextNode.Type != NodeType.Fault)
			{
				return null;
			}
			return nextNode;
		}
	}

	public Node ParentTryNode
	{
		get
		{
			Node node = _parent;
			while (node != null && node.Type != 0)
			{
				node = node.Parent;
			}
			return node;
		}
	}

	public Instruction Start
	{
		get
		{
			for (Node current = this; current != null; current = current.Children[0])
			{
				if (current.Block != null)
				{
					return current.Block.First;
				}
			}
			throw new NotSupportedException("Unsupported Node (" + this?.ToString() + ") with no children!");
		}
	}

	public Instruction End
	{
		get
		{
			if (Block != null)
			{
				return Block.Last;
			}
			if (_children.Count != 0)
			{
				return _children[_children.Count - 1].End;
			}
			throw new NotSupportedException("Unsupported Node (" + this?.ToString() + ") with no children!");
		}
	}

	internal Node(NodeType type, InstructionBlock block)
		: this(null, type, block, new Node[0], null)
	{
	}

	public Node(Node parent, NodeType type, InstructionBlock block, Node[] children, ExceptionHandler handler)
	{
		_parent = parent;
		_type = type;
		_block = block;
		_children = children.AsReadOnly();
		_handler = handler;
		Id = -1;
		if (_block != null && type != NodeType.Block)
		{
			_block.MarkIsAliveRecursive();
		}
		if (_parent != null && _parent.Type != NodeType.Root)
		{
			_block.MarkIsAliveRecursive();
		}
		bool isDead = _type != NodeType.Root;
		for (int i = 0; i < children.Length; i++)
		{
			Node child = children[i];
			child._parent = this;
			child._index = i;
			if (isDead && child.Block != null)
			{
				child._block.MarkIsAliveRecursive();
			}
		}
	}

	public bool IsChildOf(Node node)
	{
		foreach (Node childNode in node.Children)
		{
			if (childNode == this)
			{
				return true;
			}
			if (IsChildOf(childNode))
			{
				return true;
			}
		}
		return false;
	}

	public bool HasCatchOrFilterNodes()
	{
		if (_type != 0)
		{
			throw new NotSupportedException("Cannot find the related catch or filter handler for a non-try block");
		}
		for (Node current = NextSibling; current != null; current = current.NextSibling)
		{
			NodeType type = current.Type;
			if ((uint)(type - 1) <= 1u)
			{
				return true;
			}
		}
		return false;
	}

	public Node GetEnclosingFinallyOrFaultNode()
	{
		for (Node current = this; current != null; current = current.Parent)
		{
			if (current.Type == NodeType.Finally || current.Type == NodeType.Fault)
			{
				return current;
			}
		}
		return null;
	}

	private IEnumerable<Node> Walk(Func<Node, bool> filter)
	{
		Queue<Node> queue = new Queue<Node>();
		queue.Enqueue(this);
		while (queue.Count > 0)
		{
			Node current = queue.Dequeue();
			if (filter(current))
			{
				yield return current;
			}
			foreach (Node child in current.Children)
			{
				queue.Enqueue(child);
			}
		}
	}

	public override string ToString()
	{
		return $"{Enum.GetName(typeof(NodeType), _type)} children: {_children.Count}, depth: {Depth}";
	}
}

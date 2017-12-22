#if DEBUG || NUNIT
using NetTrace;
using NUnit.Framework;
#endif

namespace DAP.CompGeom
{
	////////////////////////////////////////////////////////////////////////////////////////////////////
	/// <summary>	Node in the tree representing our beachline. </summary>
	///
	/// <remarks>	Darrellp, 2/18/2011. </remarks>
	////////////////////////////////////////////////////////////////////////////////////////////////////

	abstract internal class Node
	{
		#region Private Variables
		Node _leftChild;
		Node _rightChild;
		#endregion

		#region Constructor
		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Default constructor. </summary>
		///
		/// <remarks>	Darrellp, 2/18/2011. </remarks>
		////////////////////////////////////////////////////////////////////////////////////////////////////
		protected Node()
		{
			NdParent = null;
		}

		#endregion

		#region Trace Tree
#if DEBUG || NETTRACE
		internal void TraceTree(string str, tv traceEnumElement)
		{
			Tracer.Trace(tv.Trees, str);
			TraceTreeWithIndent(traceEnumElement, 0);
		}

		abstract internal void TraceTreeWithIndent(tv traceEnumElement, int cIndent);
#endif
		#endregion

		#region Properties

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Gets our parent's other sibling. </summary>
		///
		/// <value>	The sibling. </value>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		internal Node ImmediateSibling
		{
			get
			{
				return IsLeftChild ? NdParent.RightChild : NdParent.LeftChild;
			}
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Tells whether we're our parent's left child. </summary>
		///
		/// <value>	true if is a left child, false if not. </value>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		internal bool IsLeftChild
		{
			get
			{
				return this == NdParent.LeftChild;
			}
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Gets a value indicating whether this node is leaf. </summary>
		///
		/// <value>	true if this object is leaf, false if not. </value>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		internal bool IsLeaf
		{
			get
			{
				return LeftChild == null;
			}
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Gets or sets our parent node. </summary>
		///
		/// <value>	The parent. </value>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		internal InternalNode NdParent { get; set; }

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Gets or sets our left child. </summary>
		///
		/// <value>	The left child. </value>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		internal Node LeftChild
		{
			get
			{
				return _leftChild;
			}
			set
			{
				_leftChild = value;
				_leftChild.NdParent = (InternalNode)this;
			}
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Gets or sets the right child. </summary>
		///
		/// <value>	The right child. </value>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		internal Node RightChild
		{
			get
			{
				return _rightChild;
			}
			set
			{
				_rightChild = value;
				_rightChild.NdParent = (InternalNode)this;
			}
		}
		#endregion

		#region Modification

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Remove pointers to parent and mark the node as an orphan. </summary>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		internal void SnipFromParent()
		{
			// If it's the left child of its parent
			if (IsLeftChild)
			{
				// Null the parent's left child pointer
				// Can't use LeftChild property here since it will try to set the parent of the incoming null node.
				NdParent._leftChild = null;
			}
			else
			{
				// Null the parent's right child pointer
				NdParent._rightChild = null;
			}
			// Null our own parent pointer
			NdParent = null;
		}
		#endregion

		#region NUnit
#if NUNIT || DEBUG
		[TestFixture]
		public class TestNode
		{
			[Test]
			public void TestInsertDelete()
			{
				var poly = new FortunePoly(new PointD(0, 0), 0);

				Node ndRoot = new InternalNode(poly, poly);
				Node ndLeft = new InternalNode(poly, poly);
				Node ndRight = new InternalNode(poly, poly);
				Node ndLL = new LeafNode(poly);
				Node ndLR = new LeafNode(poly);
				Node ndRL = new LeafNode(poly);
				Node ndRR = new LeafNode(poly);

				ndRoot.LeftChild = ndLeft;
				ndRoot.RightChild = ndRight;
				ndRight.LeftChild = ndRL;
				ndRight.RightChild = ndRR;
				ndLeft.LeftChild = ndLL;
				ndLeft.RightChild = ndLR;

				Assert.IsTrue(ndLeft.NdParent == ndRoot);
				Assert.IsTrue(ndRL.NdParent == ndRight);
				Assert.IsTrue(ndLL.IsLeaf);
				Assert.IsFalse(ndRoot.IsLeaf);
				Assert.IsTrue(ndLeft.IsLeftChild);
				Assert.IsFalse(ndRight.IsLeftChild);

				ndLeft.SnipFromParent();
				Assert.IsNull(ndLeft.NdParent);
				Assert.IsNull(ndRoot.LeftChild);
			}
		}
#endif
		#endregion
	}
}
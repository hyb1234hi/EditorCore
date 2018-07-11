﻿using Syroot.NintenTools.Byaml;
using Syroot.NintenTools.Byaml.Dynamic;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Syroot.BinaryData;
using EditorCore;
using EditorCore.EditorFroms;

namespace ByamlExt
{
    public partial class ByamlViewer : Form
    {
		public ByteOrder byteOrder;
		public dynamic byml;
		public string FileName = "";
		bool pathSupport;
		public ByamlViewer(System.Collections.IEnumerable by, bool _pathSupport,
			ByteOrder defaultOrder = ByteOrder.LittleEndian, string name = "")
        {
            InitializeComponent();
			byteOrder = defaultOrder;
			FileName = name;
			byml = by;
			pathSupport = _pathSupport;

			if (byml == null) return;
			//the first node should always be a dictionary node
			if (byml is Dictionary<string, dynamic>)
			{
				parseDictNode(byml, treeView1.Nodes);
			}
			else if (byml is List<dynamic>)
			{
				parseArrayNode(byml, treeView1.Nodes);
			}
			else if (byml is List<ByamlPathPoint>)
			{
				MessageBox.Show("Unsupported root node");
			}
			else throw new Exception($"Unsupported root node type {by.GetType()}");
        }

		Stream saveStream = null;
		public ByamlViewer(System.Collections.IEnumerable by, bool _pathSupport, Stream saveTo, ByteOrder defaultOrder = ByteOrder.LittleEndian) : this(by, _pathSupport,defaultOrder)
		{
			saveStream = saveTo;
			saveToolStripMenuItem.Visible = true;
		}

		//get a reference to the value to change
		class EditableNode
		{
			public Type type { get => Node[Index].GetType(); }
			dynamic Node;
			dynamic Index;

			public dynamic Get() => Node[Index];
			public void Set(dynamic value) => Node[Index] = value;
			public string GetTreeViewString()
			{
				if (Index is int)
					return Node[Index].ToString();
				else
					return Index +" : " + Node[Index].ToString();
			}

			public EditableNode(dynamic _node, dynamic _index)
			{
				Node = _node;
				Index = _index;
			}
		}
        
        void parseDictNode(IDictionary<string, dynamic> node, TreeNodeCollection addto)
        {
            foreach (string k in node.Keys)
            {
                TreeNode current = addto.Add(k);
                if (node[k] is IDictionary<string, dynamic>)
                {
                    current.Text += " : <Dictionary>";
                    current.Tag = node[k]; 
                    current.Nodes.Add("✯✯dummy✯✯"); //a text that can't be in a byml
                }
                else if (node[k] is IList<dynamic>)
                {
                    current.Text += " : <Array>";
                    current.Tag = ((IList<dynamic>)node[k]).ToList();
                    current.Nodes.Add("✯✯dummy✯✯");
				}
				else if (node[k] is IList<ByamlPathPoint>)
				{
					current.Text += " : <PathPointArray>";
					current.Tag = ((IList<ByamlPathPoint>)node[k]).ToList();
					parsePathPointArray(node[k], current.Nodes);
				}
				else
                {
                    current.Text = current.Text + " : " + (node[k] == null  ? "<NULL>" : node[k].ToString());
					if (node[k] != null) current.Tag = new EditableNode(node,k);
				}
            }
        }

		void parsePathPointArray(IList<ByamlPathPoint> list, TreeNodeCollection addto)
		{
			int index = 0;
			foreach (var k in list)
			{
				index++;
				var n = addto.Add(k == null ? "<NULL>" : k.ToString());
				if (k != null) n.Tag = new EditableNode(list, index);
			}
		}

        void parseArrayNode(IList<dynamic> list, TreeNodeCollection addto)
        {
			int index = 0;
            foreach (dynamic k in list)
            {
				index++;
				if (k is IDictionary<string, dynamic>)
                {
                    TreeNode current = addto.Add("<Dictionary>");
                    current.Tag = ((IDictionary<string, dynamic>)k);
                    current.Nodes.Add("✯✯dummy✯✯");
                }
                else if (k is IList<dynamic>)
                {
                    TreeNode current = addto.Add("<Array>");
                    current.Tag = ((IList<dynamic>)k).ToList();
                    current.Nodes.Add("✯✯dummy✯✯");
				}
				else if (k is IList<ByamlPathPoint>)
				{
					TreeNode current = addto.Add("<PathPointArray>");
					current.Tag = ((IList<ByamlPathPoint>)k).ToList();
					parsePathPointArray(k, current.Nodes);
				}
				else
                {
					var n = addto.Add(k == null ? "<NULL>" : k.ToString());
					if (k != null) n.Tag = new EditableNode(list, index);
                }
            }
        }

        private void BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            if (e.Node.Tag != null && e.Node.Nodes.Count == 1 && e.Node.Nodes[0].Text == "✯✯dummy✯✯")
            {
                e.Node.Nodes.Clear();
                if (((dynamic)e.Node.Tag).Count == 0)
                {
                    e.Node.Nodes.Add("<Empty>");
                    return;
                }
                if (e.Node.Tag is IList<dynamic>) parseArrayNode((IList<dynamic>)e.Node.Tag, e.Node.Nodes);
                else if (e.Node.Tag is IDictionary<string, dynamic>) parseDictNode((IDictionary<string, dynamic>)e.Node.Tag, e.Node.Nodes);
                else throw new Exception("WTF");
            }
        }

        private void ContextMenuOpening(object sender, CancelEventArgs e)
        {
            CopyNode.Enabled = treeView1.SelectedNode != null;
			editValueNodeMenuItem.Enabled = treeView1.SelectedNode != null && treeView1.SelectedNode.Tag is EditableNode;
		}

        private void CopyNode_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(treeView1.SelectedNode.Text);
        }

        private void ByamlViewer_Load(object sender, EventArgs e)
        {

        }

        private void exportJsonToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        public static void ImportFromJson()
        {
			
        }

        public static void OpenByml()
        {
            OpenFileDialog opn = new OpenFileDialog();
            opn.Filter = "byml file | *.byml";
            if (opn.ShowDialog() == DialogResult.OK)
            {
                OpenByml(opn.FileName);
            }
        }

		static bool SupportPaths()
		{
			return MessageBox.Show("Does this game support paths ?", "", MessageBoxButtons.YesNo) == DialogResult.Yes;
		}

        public static void OpenByml(string Filename)
        {
			bool paths = SupportPaths();

			var byml = ByamlFile.LoadGetEndianness(new FileStream(Filename, FileMode.Open, FileAccess.Read), paths);
            new ByamlViewer(byml.RootNode, paths, byml.byteOrder).Show();
		}

		public static void OpenByml(Stream file, string FileName = "")
		{
			bool paths = SupportPaths();

			var byml = ByamlFile.LoadGetEndianness(file, paths);
			new ByamlViewer(byml.RootNode, paths, byml.byteOrder, FileName).Show();
		}

		private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog sav = new SaveFileDialog() { FileName = Name, Filter = "byml file | *.byml" };
            if (sav.ShowDialog() == DialogResult.OK)
            {
                ByamlFile.Save(sav.FileName, byml, pathSupport, byteOrder);
            }
        }

		private void editValueNodeMenuItem_Click(object sender, EventArgs e)
		{
			var node = treeView1.SelectedNode.Tag as EditableNode;
			if (node == null) return;

			if (node.Get() is ByamlPathPoint)
			{
				new BymlPathPointEditor(node.Get()).ShowDialog(); //ByamlPathPoint is a reference type
			}
			else
			{
				string value = node.Get().ToString();
				var dRes = InputDialog.Show("Enter value", $"Enter new value for the node, the value must be of type {node.type}", ref value);
				if (dRes != DialogResult.OK) return;
				if (value.Trim() == "") return;
				node.Set(ByamlTypeHelper.ConvertValue(node.type, value));
			}
			treeView1.SelectedNode.Text = node.GetTreeViewString();
		}

		private void addNodeToolStripMenuItem_Click(object sender, EventArgs e)
		{
			dynamic target = treeView1.SelectedNode.Tag;
			var targetNodeCollection = treeView1.SelectedNode.Nodes;

			if (treeView1.SelectedNode == null)
			{
				target = byml;
				targetNodeCollection = treeView1.Nodes;
			}
			else if (target is EditableNode)
			{
				if (treeView1.SelectedNode.Parent == null)
				{
					target = byml;
					targetNodeCollection = treeView1.Nodes;
				}
				else
				{
					target = treeView1.SelectedNode.Parent.Tag;
					targetNodeCollection = treeView1.SelectedNode.Parent.Nodes;
				}
			}

			var newProp = AddPropertyDialog.newProperty(!(target is List<dynamic>));
			if (newProp == null) return;
			bool clone = newProp.Item2 is Dictionary<string, dynamic> || newProp.Item2 is List<dynamic>; //reference types must be manually cloned
			var toAdd = clone ? DeepCloneDictArr.DeepClone(newProp.Item2) : newProp.Item2;

			targetNodeCollection.Clear();

			if (target is List<dynamic>)
			{
				((List<dynamic>)target).Add(toAdd);
				parseArrayNode((List<dynamic>)target, targetNodeCollection);
			}
			else if (target is Dictionary<string, dynamic>)
			{
				((Dictionary<string, dynamic>)target).Add(newProp.Item1, toAdd);
				parseDictNode((Dictionary<string, dynamic>)target, targetNodeCollection);
			}
			else throw new Exception();

		}

		private void deleteNodeToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (treeView1.SelectedNode == null)
			{
				MessageBox.Show("Select a node first");
				return;
			}

			dynamic target;
			TreeNodeCollection targetNode;
			if (treeView1.SelectedNode.Parent == null)
			{
				target = byml;
				targetNode = treeView1.Nodes;
			}
			else
			{
				target = treeView1.SelectedNode.Parent.Tag;
				targetNode = treeView1.SelectedNode.Parent.Nodes;
			}
			int index = targetNode.IndexOf(treeView1.SelectedNode);
			if (target is Dictionary<string, dynamic>)
			{
				target.Remove(((Dictionary<string, dynamic>)target).Keys.ToArray()[index]);
			}
			else
				target.RemoveAt(targetNode.IndexOf(treeView1.SelectedNode));
			targetNode.RemoveAt(index);
		}

		private void saveToolStripMenuItem_Click(object sender, EventArgs e)
		{
			saveStream.Position = 0;
			saveStream.SetLength(0);
			ByamlFile.Save(saveStream, byml, pathSupport, byteOrder);
		}
	}
}
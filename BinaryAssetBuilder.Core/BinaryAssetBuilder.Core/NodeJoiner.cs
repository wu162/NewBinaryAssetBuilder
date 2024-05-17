using System;
using System.Xml;
using System.Xml.Schema;

namespace BinaryAssetBuilder.Core
{
	public class NodeJoiner
	{
		private enum JoinAction
		{
			Append = 0,
			Overwrite = 1,
			Remove = 2,
			Replace = 1
		}

		private enum InsertPosition
		{
			Top,
			Bottom
		}

		private static Tracer _Tracer = Tracer.GetTracer("NodeJoiner", "Joins XML Nodes, for inheritance and overriding");

		private readonly XmlDocument m_document;

		private readonly XmlSchemaSet m_schema;

		public static XmlNode Override(XmlSchemaSet docSchema, XmlDocument document, XmlNode baseNode, XmlNode overrideNode)
		{
			XmlNode xmlNode = document.CreateNode(baseNode.NodeType, baseNode.Name, baseNode.NamespaceURI);
			NodeJoiner nodeJoiner = new NodeJoiner(docSchema, document);
			JoinAction joinAction = JoinAction.Append;
			nodeJoiner.GetJoinAction(overrideNode, ref joinAction);
			switch (joinAction)
			{
			case JoinAction.Append:
				nodeJoiner.ReplaceXmlNode(xmlNode, baseNode, null, parentsMatch: false, JoinAction.Append);
				break;
			case JoinAction.Remove:
			{
				string text = ((overrideNode.Attributes.GetNamedItem("id") is XmlAttribute xmlAttribute) ? xmlAttribute.Value : "<Unknown id>");
				throw new BinaryAssetBuilderException(ErrorCode.InheritFromError, "Removal of top-level asset is not supported, in {0}:{1} ({2})", overrideNode.Name, text, overrideNode.OwnerDocument.BaseURI);
			}
			}
			nodeJoiner.ReplaceXmlNode(xmlNode, overrideNode, null, parentsMatch: false, joinAction);
			return xmlNode;
		}

		private XmlNode SelectSame(XmlNode destParent, XmlNode srcChild, bool idMatchRequired, bool replaceAny)
		{
			if (destParent.ChildNodes == null)
			{
				return null;
			}
			if (idMatchRequired)
			{
				foreach (XmlNode childNode in destParent.ChildNodes)
				{
					XmlNode xmlNode = SelectMatchingKey(childNode, srcChild);
					if (xmlNode != null)
					{
						return xmlNode;
					}
				}
			}
			else
			{
				if (replaceAny && destParent.ChildNodes.Count == 1)
				{
					return destParent.ChildNodes[0];
				}
				foreach (XmlNode childNode2 in destParent.ChildNodes)
				{
					if (childNode2.Name == srcChild.Name)
					{
						return childNode2;
					}
				}
			}
			return null;
		}

		public NodeJoiner(XmlSchemaSet docSchema, XmlDocument document)
		{
			m_schema = docSchema;
			m_document = document;
		}

		private XmlNode SelectMatchingKey(XmlNode destNode, XmlNode srcNode)
		{
			if (destNode.Attributes != null && srcNode.Attributes != null)
			{
				XmlAttribute xmlAttribute = srcNode.Attributes.GetNamedItem("id") as XmlAttribute;
				XmlAttribute xmlAttribute2 = destNode.Attributes.GetNamedItem("id") as XmlAttribute;
				if (xmlAttribute != null && xmlAttribute2 != null && xmlAttribute.Value == xmlAttribute2.Value)
				{
					return destNode;
				}
			}
			return null;
		}

		protected XmlSchemaObject GetObjectType(XmlNode node, XmlSchemaParticle parent)
		{
			if (node.SchemaInfo != null && node.SchemaInfo.SchemaType != null)
			{
				return node.SchemaInfo.SchemaType;
			}
			XmlQualifiedName xmlQualifiedName = new XmlQualifiedName(node.Name, node.NamespaceURI);
			if (parent != null)
			{
				XmlSchemaSequence xmlSchemaSequence = parent as XmlSchemaSequence;
				XmlSchemaObjectCollection xmlSchemaObjectCollection = null;
				if (xmlSchemaSequence != null)
				{
					xmlSchemaObjectCollection = xmlSchemaSequence.Items;
				}
				if (parent is XmlSchemaChoice xmlSchemaChoice)
				{
					xmlSchemaObjectCollection = xmlSchemaChoice.Items;
				}
				if (xmlSchemaObjectCollection != null)
				{
					foreach (XmlSchemaParticle item in xmlSchemaObjectCollection)
					{
						if (item is XmlSchemaElement)
						{
							XmlSchemaElement xmlSchemaElement = (XmlSchemaElement)item;
							if (xmlSchemaElement.QualifiedName == xmlQualifiedName)
							{
								return xmlSchemaElement.ElementSchemaType;
							}
						}
					}
				}
			}
			else if (m_schema.GlobalTypes.Contains(xmlQualifiedName))
			{
				return m_schema.GlobalTypes[xmlQualifiedName];
			}
			return null;
		}

		private bool FindPrevNode(XmlSchemaComplexType complexType, XmlNode n, XmlSchemaElement element)
		{
			if (complexType.BaseXmlSchemaType != null && complexType.BaseXmlSchemaType is XmlSchemaComplexType complexType2 && FindPrevNode(complexType2, n, element))
			{
				return true;
			}
			if (!(complexType.ContentTypeParticle is XmlSchemaSequence xmlSchemaSequence) || xmlSchemaSequence.Items == null)
			{
				return false;
			}
			foreach (XmlSchemaParticle item in xmlSchemaSequence.Items)
			{
				if (item is XmlSchemaElement xmlSchemaElement && n.Name == xmlSchemaElement.Name)
				{
					return true;
				}
				if (element == item)
				{
					return false;
				}
			}
			return false;
		}

		private XmlSchemaElement FindInBase(XmlSchemaComplexType complexType, XmlQualifiedName childQualName)
		{
			if (complexType.BaseXmlSchemaType != null && complexType.BaseXmlSchemaType is XmlSchemaComplexType complexType2)
			{
				XmlSchemaElement xmlSchemaElement = FindInBase(complexType2, childQualName);
				if (xmlSchemaElement != null)
				{
					return xmlSchemaElement;
				}
			}
			if (!(complexType.ContentTypeParticle is XmlSchemaSequence xmlSchemaSequence) || xmlSchemaSequence.Items == null)
			{
				return null;
			}
			foreach (XmlSchemaParticle item in xmlSchemaSequence.Items)
			{
				if (item is XmlSchemaElement xmlSchemaElement2 && xmlSchemaElement2.QualifiedName == childQualName)
				{
					return xmlSchemaElement2;
				}
			}
			return null;
		}

		private XmlNode AppendCorrespondedXmlNode(XmlNode XmlParentNode, XmlNode srcChild, XmlSchemaElement element, XmlSchemaComplexType parentType, InsertPosition insertPos)
		{
			XmlNode xmlNode = m_document.CreateNode(srcChild.NodeType, srcChild.Name, srcChild.NamespaceURI);
			if (element != null && element.Parent is XmlSchemaSequence && XmlParentNode.HasChildNodes)
			{
				_ = (XmlSchemaSequence)element.Parent;
				XmlNode refChild = null;
				bool flag = false;
				foreach (XmlNode childNode in XmlParentNode.ChildNodes)
				{
					if (childNode.LocalName == element.Name)
					{
						if (insertPos != InsertPosition.Bottom)
						{
							refChild = null;
							break;
						}
						refChild = childNode;
						flag = true;
					}
					else
					{
						if (flag)
						{
							break;
						}
						if (parentType != null && FindPrevNode(parentType, childNode, element))
						{
							refChild = childNode;
						}
					}
				}
				XmlParentNode.InsertAfter(xmlNode, refChild);
			}
			else if (insertPos == InsertPosition.Bottom)
			{
				XmlParentNode.AppendChild(xmlNode);
			}
			else
			{
				XmlParentNode.InsertAfter(xmlNode, null);
			}
			return xmlNode;
		}

		private void GetJoinAction(XmlNode theNode, ref JoinAction joinAction)
		{
			XmlAttribute xmlAttribute = theNode.Attributes["joinAction", "uri:ea.com:eala:asset:instance"];
			if (xmlAttribute != null)
			{
				try
				{
					joinAction = (JoinAction)Enum.Parse(typeof(JoinAction), xmlAttribute.Value, ignoreCase: true);
				}
				catch (Exception innerException)
				{
					throw new BinaryAssetBuilderException(innerException, ErrorCode.SchemaValidation, "{0} not valid for joinAction, in {1}.  Valid values: Append, Replace, Remove", xmlAttribute.Value, theNode.Name);
				}
			}
		}

		private void GetInsertPosition(XmlNode theNode, ref InsertPosition insertPos)
		{
			XmlAttribute xmlAttribute = theNode.Attributes["insertPosition", "uri:ea.com:eala:asset:instance"];
			if (xmlAttribute != null)
			{
				try
				{
					insertPos = (InsertPosition)Enum.Parse(typeof(InsertPosition), xmlAttribute.Value, ignoreCase: true);
				}
				catch (Exception innerException)
				{
					throw new BinaryAssetBuilderException(innerException, ErrorCode.SchemaValidation, "{0} not valid for insertPosition, in {1}.  Valid values: Top, Bottom", xmlAttribute.Value, theNode.Name);
				}
			}
		}

		private void ReplaceXmlNode(XmlNode dest, XmlNode src, XmlSchemaParticle parent, bool parentsMatch, JoinAction parentAction)
		{
			XmlSchemaObject objectType = GetObjectType(src, parent);
			XmlSchemaComplexType xmlSchemaComplexType = objectType as XmlSchemaComplexType;
			if (src.ChildNodes != null && src.ChildNodes.Count != 0)
			{
				if (objectType == null)
				{
					throw new BinaryAssetBuilderException(ErrorCode.SchemaValidation, $"The element {src.Name} is not expected in {src.ParentNode.Name}");
				}
				foreach (XmlNode childNode in src.ChildNodes)
				{
					XmlQualifiedName xmlQualifiedName = new XmlQualifiedName(childNode.Name, childNode.NamespaceURI);
					if (childNode is XmlDeclaration || childNode is XmlComment)
					{
						continue;
					}
					if (childNode is XmlText)
					{
						XmlNode xmlNode2 = m_document.CreateNode(childNode.NodeType, childNode.Name, childNode.NamespaceURI);
						xmlNode2.Value = childNode.Value;
						dest.AppendChild(xmlNode2);
						continue;
					}
					if (xmlSchemaComplexType == null)
					{
						throw new BinaryAssetBuilderException(ErrorCode.SchemaValidation, $"The element {src.Name} is not expected in {src.ParentNode.Name}");
					}
					XmlSchemaElement xmlSchemaElement = FindInBase(xmlSchemaComplexType, xmlQualifiedName);
					bool flag = xmlSchemaComplexType.ContentTypeParticle is XmlSchemaSequence;
					bool flag2 = xmlSchemaComplexType.ContentTypeParticle is XmlSchemaChoice;
					if (xmlSchemaElement == null && flag)
					{
						throw new BinaryAssetBuilderException(ErrorCode.XmlFormattingError, "Bad XML: unexpected {0} in {1}", childNode.Name, xmlSchemaComplexType.Name);
					}
					XmlNode xmlNode3 = null;
					bool replaceAny = flag2 && xmlSchemaComplexType.ContentTypeParticle.MaxOccurs <= 1m;
					bool idMatchRequired = (flag && xmlSchemaElement.MaxOccurs > 1m) || (flag2 && xmlSchemaComplexType.ContentTypeParticle.MaxOccurs > 1m);
					xmlNode3 = SelectSame(dest, childNode, idMatchRequired, replaceAny);
					bool parentsMatch2 = parentsMatch || xmlNode3 != null;
					JoinAction joinAction = parentAction;
					GetJoinAction(childNode, ref joinAction);
					InsertPosition insertPos = InsertPosition.Bottom;
					GetInsertPosition(childNode, ref insertPos);
					string text = ((src.Attributes.GetNamedItem("id") is XmlAttribute xmlAttribute) ? xmlAttribute.Value : "<Unknown id>");
					string text2 = ((childNode.Attributes.GetNamedItem("id") is XmlAttribute xmlAttribute2) ? xmlAttribute2.Value : "<Unknown id>");
					if (xmlNode3 != null)
					{
						XmlQualifiedName xmlQualifiedName2 = new XmlQualifiedName(xmlNode3.Name, xmlNode3.NamespaceURI);
						if (xmlQualifiedName2 != xmlQualifiedName || joinAction == JoinAction.Remove || joinAction == JoinAction.Overwrite)
						{
							dest.RemoveChild(xmlNode3);
							xmlNode3 = null;
						}
					}
					else if (joinAction == JoinAction.Remove)
					{
						throw new BinaryAssetBuilderException(ErrorCode.InheritFromError, "{0}:{1} in {2}:{3} attempts to remove non-existent node", childNode.Name, text2, src.Name, text);
					}
					if (joinAction != JoinAction.Remove)
					{
						if (xmlNode3 == null)
						{
							xmlNode3 = AppendCorrespondedXmlNode(dest, childNode, xmlSchemaElement, xmlSchemaComplexType, insertPos);
						}
						ReplaceXmlNode(xmlNode3, childNode, xmlSchemaComplexType.ContentTypeParticle, parentsMatch2, joinAction);
					}
				}
			}
			if (src.Value != null)
			{
				dest.Value = src.Value;
			}
			if (src.Attributes == null)
			{
				return;
			}
			foreach (XmlAttribute attribute in src.Attributes)
			{
				if (!(attribute.NamespaceURI == "xmlns") && !(attribute.LocalName == "xmlns") && !(attribute.LocalName == "TypeId") && (!(attribute.NamespaceURI == "uri:ea.com:eala:asset:instance") || (!(attribute.LocalName == "insertPosition") && !(attribute.LocalName == "joinAction"))))
				{
					XmlAttribute destAttrib = dest.Attributes[attribute.LocalName];
					if (destAttrib == null)
					{
						destAttrib = m_document.CreateAttribute(attribute.Name, attribute.NamespaceURI);
						dest.Attributes.Append(destAttrib);
					}
					ReplaceNodeAttributes(attribute, xmlSchemaComplexType, ref destAttrib, ref dest);
				}
			}
		}

		private void ReplaceNodeAttributes(XmlAttribute srcAttrib, XmlSchemaComplexType complexType, ref XmlAttribute destAttrib, ref XmlNode dest)
		{
			bool flag = true;
			XmlQualifiedName name = new XmlQualifiedName(srcAttrib.Name);
			if (complexType != null && complexType.AttributeUses != null && complexType.AttributeUses.Contains(name) && complexType.AttributeUses[name] is XmlSchemaAttribute xmlSchemaAttribute && xmlSchemaAttribute.AttributeSchemaType.Content is XmlSchemaSimpleTypeList xmlSchemaSimpleTypeList && xmlSchemaSimpleTypeList != null && xmlSchemaSimpleTypeList.BaseItemType.Content is XmlSchemaSimpleTypeRestriction xmlSchemaSimpleTypeRestriction)
			{
				XmlSchemaObjectEnumerator enumerator = xmlSchemaSimpleTypeRestriction.Facets.GetEnumerator();
				bool flag2 = true;
				while (enumerator.MoveNext())
				{
					object current = enumerator.Current;
					if (!(current is XmlSchemaEnumerationFacet))
					{
						flag2 = false;
						break;
					}
				}
				if (flag2 && (srcAttrib.Value.Contains("+") || srcAttrib.Value.Contains("-")))
				{
					flag = false;
					string[] array = srcAttrib.Value.Split(' ');
					string[] array2 = array;
					foreach (string text in array2)
					{
						string text2 = text[0].ToString();
						string text3 = text.Substring(1);
						switch (text2)
						{
						case "+":
							if (!destAttrib.Value.Contains(text3))
							{
								XmlAttribute obj = destAttrib;
								obj.Value = obj.Value + " " + text3;
							}
							break;
						case "-":
						{
							if (destAttrib.Value.Contains(text3))
							{
								destAttrib.Value = destAttrib.Value.Replace(text3, "");
								break;
							}
							string message2 = $"Invalid removal of bitflag {text3} from attribute {destAttrib.Name} in node {dest.Name} in document {m_document.BaseURI}";
							throw new BinaryAssetBuilderException(ErrorCode.InheritFromError, message2);
						}
						default:
						{
							string message = string.Format("Illegal form for +/- override.  Modifier {0} either missing or not recognized. Attribute {1} in node {2} in document {3}", text2 + "" + text3, destAttrib.Name, dest.Name, m_document.BaseURI);
							throw new BinaryAssetBuilderException(ErrorCode.InheritFromError, message);
						}
						}
					}
				}
			}
			if (flag)
			{
				destAttrib.Value = srcAttrib.Value;
			}
		}
	}
}

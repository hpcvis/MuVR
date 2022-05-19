using UnityEngine;
using UnityEditor;

public class TypeConstraintAttribute : PropertyAttribute
{
	private System.Type type;
 
	public TypeConstraintAttribute(System.Type type)
	{
		this.type = type;
	}
 
	public System.Type Type
	{
		get { return type; }
	}
}
 

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(TypeConstraintAttribute))]
public class TypeConstraintDrawer : PropertyDrawer
{
	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	{
		if (property.propertyType != SerializedPropertyType.ObjectReference)
		{
			// Show error
			// Also check that the user only uses the attribute on a GameObject or Component
			// because we need to call GetComponent
		}
 
		var constraint = attribute as TypeConstraintAttribute;
 
		if (DragAndDrop.objectReferences.Length > 0)
		{
			var draggedObject = DragAndDrop.objectReferences[0] as GameObject;
 
			// Prevent dragging of an object that doesn't contain the interface type.
			if (draggedObject is null || (draggedObject is not null && draggedObject.GetComponent(constraint.Type) is null))
				DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
		}
 
		// If a value was set through other means (e.g. ObjectPicker)
		if(property.objectReferenceValue != null)
		{
			// Check if the interface is present.
			GameObject go = property.objectReferenceValue as GameObject;
			if (go is not null && go.GetComponent(constraint.Type) is null)
			{
				// Clean out invalid references.
				property.objectReferenceValue = null;
			}
		}
             
		property.objectReferenceValue = EditorGUI.ObjectField(position, label, property.objectReferenceValue, typeof(GameObject), true);
	}
}
#endif
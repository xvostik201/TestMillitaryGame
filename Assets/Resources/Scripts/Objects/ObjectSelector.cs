using UnityEngine;
using UnityEngine.EventSystems;

public class ObjectSelector : MonoBehaviour, IPointerClickHandler
{
    private ObjectSpawner _spawner;
    private ObjectSpawner.SpawnedObjectData _data;

    public void Initialize(ObjectSpawner spawner, ObjectSpawner.SpawnedObjectData data)
    {
        _spawner = spawner;
        _data = data;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        _spawner.SelectObjectForEditing(gameObject, _data);
    }
}

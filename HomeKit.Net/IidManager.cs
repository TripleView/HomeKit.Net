namespace HomeKit.Net;

/// <summary>
/// Maintains a mapping between Service/Characteristic objects and IIDs;维护服务/特征对象和 IID 之间的映射。
/// </summary>
public class IidManager
{
    private int Counter;
    private Dictionary<IAssignIid, int> mapping;

    public IidManager()
    {
        Counter = 0;
        mapping = new Dictionary<IAssignIid, int>();
    }

    private int GetNewId()
    {
        Counter++;
        return Counter;
    }

    /// <summary>
    /// Assign an IID to given object;给对象分配分配iid
    /// </summary>
    /// <param name="obj"></param>
    /// <typeparam name="T"></typeparam>
    public void Assign<T>(T obj) where T : IAssignIid
    {
        if (!mapping.ContainsKey(obj))
        {
            var iid = GetNewId();
            mapping[obj] = iid;
        }
    }

    /// <summary>
    /// Get the object that is assigned the given IID;获取已分配iid的对象
    /// </summary>
    /// <param name="iid"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public IAssignIid GetObject(int iid)
    {
        foreach (var pair in mapping)
        {
            if (pair.Value == iid)
            {
                return pair.Key;
            }
        }

        throw new Exception($"can not find object with iid{iid}");
    }

    /// <summary>
    /// Get the IID assigned to the given object;获取已分配iid的对象的iid
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public int GetIid(IAssignIid obj)
    {
        foreach (var pair in mapping)
        {
            if (pair.Key == obj)
            {
                return pair.Value;
            }
        }

        throw new Exception($"can not find iid with object {obj.ToString()}");
    }

    /// <summary>
    /// Remove an object from the mapping;从映射中移除对象
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public int RemoveObject(IAssignIid obj)
    {
        if (mapping.ContainsKey(obj))
        {
            var iid = mapping[obj];
            mapping.Remove(obj);
            return iid;
        }
        throw new Exception($"can not find iid with object  {obj.ToString()}");
    }

    /// <summary>
    /// Remove an object with an IID from the mapping；通过iid从映射中移除对象
    /// </summary>
    /// <param name="iid"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public IAssignIid RemoveIid(int iid)
    {
        IAssignIid key = null;
        foreach (var pair in mapping)
        {
            if (pair.Value == iid)
            {
                key = pair.Key;
            }
        }

        if (key != null)
        {
            mapping.Remove(key);
            return key;
        }

        throw new Exception($"can not find iid with iid  {iid}");
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using CSObjectWrapEditor;
using System.Reflection;
using UnityEditor;
using System.Collections;

[AttributeUsage(AttributeTargets.Class)]
public class ScriptableObjectTypeAttribute : PropertyAttribute
{
    public string menuName;
    public string label;
    public ScriptableObjectTypeAttribute(string menu, string label = null)
    {
        this.menuName = menu;
        this.label = label;
    }
}

[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
public class ScriptableObjectNameAttribute : PropertyAttribute
{
    public string[] label;
    public ScriptableObjectNameAttribute(params string[] label)
    {
        this.label = label;
    }
}

[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
public class ScriptableObjectEnumAttribute : PropertyAttribute
{
    public string[] vals;
    public ScriptableObjectEnumAttribute(params string[] vals)
    {
        this.vals = vals;
    }
}

[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
public class ScriptableObjectRangeAttribute : PropertyAttribute
{
    public float min;
    public float max;
    
    public ScriptableObjectRangeAttribute(float min, float max = float.MaxValue)
    {
        this.min = min;
        this.max = max;
    }
}

[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
public class ScriptableObjectVecLenAttribute : PropertyAttribute
{
    public int len;

    public ScriptableObjectVecLenAttribute(int len)
    {
        this.len = len;
    }
}

[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
public class ScriptableObjectCommentAttribute : PropertyAttribute
{
    public ScriptableObjectCommentAttribute()
    {
    }
}

[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
public class ScriptableObjectPrimaryKeyAttribute : PropertyAttribute
{
    public ScriptableObjectPrimaryKeyAttribute()
    {
    }
}

[System.Serializable]
public class JustCollapse
{
    public bool collapse;
}

[System.Serializable]
[ScriptableObjectTypeAttribute("CreateTTest", "用于测试")]
public class TTest : JustCollapse
{
    [ScriptableObjectNameAttribute("Id")]
    public int id;

    [ScriptableObjectNameAttribute("字符串")]
    public string s;

    [ScriptableObjectNameAttribute("下拉")]
    [ScriptableObjectEnumAttribute("一", "二", "三")]
    public int i;

    [ScriptableObjectNameAttribute("对应一", "对应二", "对应三")]
    public List<int> ints;

    public Vector3 vec3;

    public bool bol;

    [ScriptableObjectRangeAttribute(-1, 4)]
    public int iv;

    [ScriptableObjectCommentAttribute()]
    public string comment;

    public TTest()
    {
        ints = new List<int>();
    }
}

public class GenerateClass
{
    [MenuItem("XLua/GenClassEditor")]
    public static void GenerateClassEditor()
    {
        foreach (var processType in (from type in XLua.Utils.GetAllTypes(false) where type.IsDefined(typeof(ScriptableObjectTypeAttribute)) select type))
        {
            StreamWriter textWriter = new StreamWriter(Application.dataPath + "/Editor/Multi" + processType.Name + ".cs", false, Encoding.UTF8);
            Generator.GenOne(processType, (type, type_info) =>
            {
                var fieldList = processType.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.IgnoreCase | BindingFlags.DeclaredOnly).ToList();
                type_info.Set<string, string>("ClassName", processType.Name);
                type_info.Set<string, List<FieldInfo>>("Fields", fieldList);
                type_info.Set<string, List<FieldInfo>>("ListFields", fieldList.Where(info => { return typeof(IList).IsAssignableFrom(info.FieldType); }).ToList());

                var tbl = Generator.luaenv.NewTable();
                fieldList.ForEach(fi =>
                {
                    var sub = Generator.luaenv.NewTable();
                    tbl.Set<string, XLua.LuaTable>(fi.Name, sub);
                    fi.CustomAttributes.ToList().ForEach((cad) =>
                    {
                        sub.Set<string, Attribute>(cad.AttributeType.Name, fi.GetCustomAttribute(cad.AttributeType, false));
                    });
                });
                type_info.Set<string, XLua.LuaTable>("Attri", tbl);

                ScriptableObjectTypeAttribute attribute = null;
                if ((attribute = processType.GetCustomAttribute<ScriptableObjectTypeAttribute>(false)) != null)
                {
                    type_info.Set<string, string>("MenuName", attribute.menuName);
                    type_info.Set<string, string>("Label", attribute.label);
                }
                else
                {
                    type_info.Set<string, string>("MenuName", processType.Name);
                    type_info.Set<string, string>("Label", "undefine");
                }

            }, Generator.templateRef.GenClassEditor, textWriter);
            textWriter.Close();
        }
    }
}

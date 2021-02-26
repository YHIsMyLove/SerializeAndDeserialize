using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace SerializeAndDeserialize
{
    partial class Program
    {
        /// <summary>
        /// 写在前面：
        ///     该示例程序的本意是，将C#的类型序列化成抽象语言,通过序列化交换数据,
        ///     给每个对象生产一个hash作为实例ID用于取得实例.这样就可以跨语言调用
        ///     类似于RPC...
        ///     ToDoList:
        ///         1:  完成基本对象的序列化/反序列化
        ///         2： 方法的调用
        ///         3： 回调函数
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            var test1 = new Test()
            {
                Name = "Danny",
                Age = 4,
                Items = new string[] { "2", "3", "5" },
                Items2 = new List<Item>()
                {
                    new Item(){ Name = "Fuck"}
                }
            };
            var getter = new ObjectDynamicManager<Test>();
            var p1 = getter.Properties[0];
            getter.SetValue(test1, p1.Name, "ffffff");
            foreach (var item in getter.Properties)
            {
                var test_name = getter.GetValue(test1, item.Name);
                Console.WriteLine($"{item.Name}:{test_name}");
            }
            Console.ReadKey();
        }
        class Test
        {
            public string Name { get; set; }
            public int Age { get; set; }
            public string[] Items { get; set; }
            public List<Item> Items2 { get; set; }
            public string TestMethodWithStringResult()
            {
                return "fuck";
            }
        }

        class Item
        {
            public string Name { get; set; }
        }

        /// <summary>Object动态操作管理类</summary>
        /// <typeparam name="T"></typeparam>
        public class ObjectDynamicManager<T>
        {
            /// <summary>缓存获取方法</summary>
            private static Func<T, string, object> cachedGetDelegate;
            /// <summary>缓存设置方法</summary>
            private static Action<T, string, object> cachedSetDelegate;
            public PropertyInfo[] Properties { get; }
            public ObjectDynamicManager()
            {
                Properties = typeof(T).GetProperties();
                cachedGetDelegate = BuildGetDelegate(Properties);
                cachedSetDelegate = BuildSetDelegate(Properties);
            }
            /// <summary>
            /// 获取值
            /// </summary>
            /// <param name="obj">对象</param>
            /// <param name="propertyName">属性名</param>
            /// <returns>属性值</returns>
            public object GetValue(T obj, string propertyName)
            {
                return cachedGetDelegate(obj, propertyName);
            }
            /// <summary>
            /// 设置值
            /// </summary>
            /// <param name="obj">对象</param>
            /// <param name="propertyName">属性名</param>
            /// <param name="value">值</param>
            public void SetValue(T obj, string propertyName, object value)
            {
                cachedSetDelegate(obj, propertyName, value);
            }

            private Func<T, string, object> BuildGetDelegate(PropertyInfo[] properties)
            {
                var objParamExpression = Expression.Parameter(typeof(T), "obj");
                var nameParamExpression = Expression.Parameter(typeof(string), "name");
                var variableExpression = Expression.Variable(typeof(object), "propertyValue");
                SwitchCase[] switchCases = new SwitchCase[properties.Length];
                for (int i = 0; i < properties.Length; i++)
                {
                    var property = properties[i];

                    var getPropertyExpression = Expression.Property(objParamExpression, property);
                    var convertPropertyExpression = Expression.Convert(getPropertyExpression, typeof(object));
                    var assignExpression = Expression.Assign(variableExpression, convertPropertyExpression);
                    var switchCase = Expression.SwitchCase(assignExpression, Expression.Constant(property.Name));
                    switchCases[i] = switchCase;
                }
                //set null when default
                var defaultBodyExpression = Expression.Assign(variableExpression, Expression.Constant(null));
                var switchExpression = Expression.Switch(nameParamExpression, defaultBodyExpression, switchCases);
                var blockExpression = Expression.Block(typeof(object), new[] { variableExpression }, switchExpression);
                var lambdaExpression = Expression.Lambda<Func<T, string, object>>(blockExpression, objParamExpression, nameParamExpression);
                return lambdaExpression.Compile();
            }
            private Action<T, string, object> BuildSetDelegate(PropertyInfo[] properties)
            {
                var objParamExpression = Expression.Parameter(typeof(T), "obj");
                var nameParamExpression = Expression.Parameter(typeof(string), "name");
                var valueExpression = Expression.Parameter(typeof(object), "setValue");
                var variableExpression = Expression.Variable(typeof(object), "propertyValue");

                SwitchCase[] switchCases = new SwitchCase[properties.Length];
                for (int i = 0; i < properties.Length; i++)
                {
                    var property = properties[i];
                    var getPropertyExpression = Expression.Property(objParamExpression, property);
                    var convertPropertyExpression = Expression.Convert(valueExpression, property.PropertyType);
                    var assignExpression = Expression.Assign(getPropertyExpression, convertPropertyExpression);
                    var switchbody = Expression.Block(typeof(void), assignExpression);
                    var switchCase = Expression.SwitchCase(switchbody, Expression.Constant(property.Name));
                    switchCases[i] = switchCase;
                }

                var switchExpression = Expression.Switch(nameParamExpression, switchCases);
                var blockExpression = Expression.Block(typeof(void), new[] { variableExpression }, switchExpression);
                var lambdaExpression = Expression.Lambda<Action<T, string, object>>(blockExpression, objParamExpression, nameParamExpression, valueExpression);
                return lambdaExpression.Compile();
            }
        }
    }
}

using InformationSystem.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Reflection.Emit;
using System.Web.Http;

namespace InformationSystem.Controllers
{
    public class InfoSystemController : ApiController
    {
        Dictionary<string, int> dicDbClasses = new Dictionary<string, int>();
        string strConn = @"Data Source=<Имя сервера>;Initial Catalog=neolant.InfoSystem;Integrated Security=True";

        // GET: api/InfoSystem/GetObjectTree/50
        [HttpGet]
        public IEnumerable<ObjectHierarchy> GetObjectTree(int id)
        {
            List<ObjectHierarchy> retHierarhy = new List<ObjectHierarchy>();
            using (SqlConnection conn = new SqlConnection(strConn))
            {
                using (SqlCommand cmdHierarhy = new SqlCommand("[dbo].[ObjectSubHierarchy]", conn)
                {
                    CommandType = System.Data.CommandType.StoredProcedure
                })
                {
                    cmdHierarhy.Parameters.AddWithValue("@ObjectId", id);
                    conn.Open();
                    using (SqlDataReader rdrHierarhy = cmdHierarhy.ExecuteReader())
                    {
                        while (rdrHierarhy.Read())
                        {
                            IDataRecord record = (IDataRecord)rdrHierarhy;
                            retHierarhy.Add(new ObjectHierarchy()
                            {
                                Name = rdrHierarhy["Name"].ToString(),
                                Level = (int)rdrHierarhy["Level"],
                                ParentId = (int?)rdrHierarhy["ParentId"],
                                Id = (int)rdrHierarhy["Id"],
                            });
                        }
                        rdrHierarhy.Close();
                    }
                }
            }
            return retHierarhy.AsEnumerable();
        }

        // GET: api/InfoSystem/GetObject/5
        public object GetObject(int id)
        {
            string PropName = null, PropData = null;

            using (SqlConnection conn = new SqlConnection(strConn))
            {
                using (SqlCommand cmdHierarhy = new SqlCommand("[dbo].[GetObjectData]", conn)
                {
                    CommandType = System.Data.CommandType.StoredProcedure
                })
                {
                    cmdHierarhy.Parameters.AddWithValue("@ObjectId", id);
                    conn.Open();
                    using (SqlDataReader rdrHierarhy = cmdHierarhy.ExecuteReader())
                    {
                        while (rdrHierarhy.Read())
                        {
                            IDataRecord record = (IDataRecord)rdrHierarhy;
                            PropName = record["Name"].ToString();
                            PropData = record["Data"].ToString();
                        }
                        rdrHierarhy.Close();
                    }
                }
            }
            //
            if (!string.IsNullOrWhiteSpace(PropName) && !string.IsNullOrWhiteSpace(PropData))
            {
                var subObjext=JsonConvert.DeserializeObject(PropData);
                var mainObject = new JObject(new JProperty(PropName, subObjext));
                return mainObject;
            }
            //
            return null;
        }
        //api/InfoSystem/GetPumps/57
        [HttpGet]
        public List<object> GetPumps(int id)
        {
            List<object> retPumps = new List<object>();
            List<PumpListItem> pumpsList = new List<PumpListItem>();
            Dictionary<string, string> tempObjectContainer = new Dictionary<string, string>();

            AssemblyName aName = new AssemblyName("DynamicAssemblyExample");
            AssemblyBuilder ab = AppDomain.CurrentDomain.DefineDynamicAssembly(aName, AssemblyBuilderAccess.RunAndSave);
            ModuleBuilder mb = ab.DefineDynamicModule(aName.Name, aName.Name + ".dll");
            TypeBuilder tbCurrent= mb.DefineType("TempPump", TypeAttributes.Public);
            //
            // Выбор свех данных
            //
            using (SqlConnection conn = new SqlConnection(strConn))
            {
                using (SqlCommand cmdHierarhy = new SqlCommand("[dbo].[GetPumps]", conn)
                {
                    CommandType = System.Data.CommandType.StoredProcedure
                })
                {
                    cmdHierarhy.Parameters.AddWithValue("@ObjectId", id);
                    cmdHierarhy.Parameters.AddWithValue("@ClassId", 2);
                    conn.Open();
                    using (SqlDataReader rdrHierarhy = cmdHierarhy.ExecuteReader())
                    {
                        while (rdrHierarhy.Read())
                        {
                            IDataRecord record = (IDataRecord)rdrHierarhy;
                            pumpsList.Add(new PumpListItem()
                            {
                                Name = rdrHierarhy["Name"].ToString(),
                                Value = JsonConvert.DeserializeObject(rdrHierarhy["Data"].ToString())
                            });
                        }
                        if (rdrHierarhy.NextResult())
                        {
                            //Формирование временного типа
                            while (rdrHierarhy.Read())
                            {
                                IDataRecord record = (IDataRecord)rdrHierarhy;
                                tempObjectContainer.Add(record["Name"].ToString(), record["Type"].ToString());
                            }
                        }
                    }
                }
            }
            if (tempObjectContainer.Count()> 0)
            {
                //Создание временного типа
                foreach (KeyValuePair<string,string> field in tempObjectContainer)
                {
                    tbCurrent.DefineField(field.Key, Type.GetType(field.Value), FieldAttributes.Public);
                }
                Type tempType = tbCurrent.CreateType();
                foreach (PumpListItem liPump in pumpsList)
                {
                    double weightPump;
                    DateTime datePump;
                    //создание временного типа
                    var subObject = JsonConvert.DeserializeObject(liPump.Value.ToString(), tempType);
                    //Фильтрация по массе и дате установки
                    if (double.TryParse(subObject.GetType().GetField("Масса").GetValue(subObject).ToString(), out weightPump))
                    {
                        if (DateTime.TryParse(subObject.GetType().GetField("Дата установки").GetValue(subObject).ToString(), out datePump))
                        {
                            if (weightPump > 10 && datePump < DateTime.Now)
                            {
                                //Создание объектов в формате "Имя объекта(насос):{ Данные в JSon формате }"
                                var mainObject = new JObject(new JProperty(liPump.Name, JsonConvert.DeserializeObject(liPump.Value.ToString())));
                                retPumps.Add(mainObject);
                            }
                        }
                    };
                }
            }
            return retPumps;
        }


        [HttpPost]
        // POST: api/InfoSystem/CreateObject/
        public int? CreateObject( CreateInfoObject newObject)
        {
            int? objectId=null;
            Dictionary<string, string> typeInfo = new Dictionary<string, string>();

            if (newObject != null && newObject.idClass != 0)
            {
                using (SqlConnection conn = new SqlConnection(strConn))
                {
                    using (SqlCommand cmdHierarhy = new SqlCommand("[dbo].[GetClassParams]", conn)
                    {
                        CommandType = System.Data.CommandType.StoredProcedure
                    })
                    {
                        cmdHierarhy.Parameters.AddWithValue("@ClassId", newObject.idClass);
                        conn.Open();
                        using (SqlDataReader rdrHierarhy = cmdHierarhy.ExecuteReader())
                        {
                            while (rdrHierarhy.Read())
                            {
                                IDataRecord record = (IDataRecord)rdrHierarhy;
                                typeInfo.Add(record["Name"].ToString(), record["Type"].ToString());
                            }
                        }
                    }
                }
            }
            if (typeInfo.Count() > 0)
            {
                AssemblyName aName = new AssemblyName("DynamicAssemblyExample");
                AssemblyBuilder ab = AppDomain.CurrentDomain.DefineDynamicAssembly(aName, AssemblyBuilderAccess.RunAndSave);
                ModuleBuilder mb = ab.DefineDynamicModule(aName.Name, aName.Name + ".dll");
                TypeBuilder tbCurrent = mb.DefineType("TempObject", TypeAttributes.Public);
                foreach (KeyValuePair<string, string> field in typeInfo)
                {
                    tbCurrent.DefineField(field.Key, Type.GetType(field.Value), FieldAttributes.Public);
                }
                Type tempType = tbCurrent.CreateType();
                object tempObject = Activator.CreateInstance(tempType);
                foreach (FieldInfo fiObject in tempType.GetFields())
                {
                    Models.Attribute at = newObject.Params.Where(pi => pi.Name == fiObject.Name).FirstOrDefault();
                    if(at!=null)
                    {
                        if (fiObject.FieldType == typeof(double))
                        {
                            double newDouble;
                            if (double.TryParse(at.Value, out newDouble))
                            {
                                fiObject.SetValue(tempObject, newDouble);
                            };
                        }
                        else
                        if (fiObject.FieldType == typeof(DateTime))
                        {
                            DateTime newDate;
                            if (DateTime.TryParse(at.Value, out newDate)) {
                                fiObject.SetValue(tempObject, newDate);
                            }
                        }
                        else if (fiObject.FieldType == typeof(string))
                        {
                            fiObject.SetValue(tempObject, at.Value);
                        }
                    }
                }
                string dataJsonString = JsonConvert.SerializeObject(tempObject);
                using (SqlConnection conn = new SqlConnection(strConn))
                {
                    using (SqlCommand cmdHierarhy = new SqlCommand("[dbo].[InsertNewObject]", conn)
                        {
                            CommandType = System.Data.CommandType.StoredProcedure
                        })
                    {
                        objectId = 0;
                        cmdHierarhy.Parameters.AddWithValue("@Name", newObject.Name);
                        cmdHierarhy.Parameters.AddWithValue("@Data", dataJsonString);
                        cmdHierarhy.Parameters.AddWithValue("@ClassId", newObject.idClass);
                        cmdHierarhy.Parameters.AddWithValue("@StartParent", newObject.idParent);
                        cmdHierarhy.Parameters.AddWithValue("@ChildID", newObject.ChildId);
                        conn.Open();
                        object o1= cmdHierarhy.ExecuteScalar();
                        objectId = (int?)o1;
                    }
                }
            }
            
            return objectId;
        }

        
    }
}

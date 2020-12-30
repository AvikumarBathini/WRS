using System.Collections.Generic;

namespace WRS.Xrm
{
    
    public partial class Contact
    {
        public class OptionSetEnums
        { 
           
            public enum address1_addresstypecode
            {
                Bill_To = 1,
                Ship_To = 2,
                Primary = 3,
                Other = 4
            }
          
        }
    }

    public partial class Store
    {
        public class OptionSetEnums
        {

            public enum wrs_defaultlanguage
            {
                English = 1,
                Chinese = 2
            }

        }
    }
    /*End Attribute NameConstants*/
}


namespace System.Linq
{
    public static class MyLinqExtensions
    {

        public static IEnumerable<IEnumerable<T>> Split<T>(this IEnumerable<T> collection, int size)
        {
            var chunks = new List<List<T>>();
            var chunkCount = collection.Count() / size;

            if (collection.Count() % size > 0)
                chunkCount++;

            for (var i = 0; i < chunkCount; i++)
                chunks.Add(collection.Skip(i * size).Take(size).ToList());

            return chunks;
        }
    }
}

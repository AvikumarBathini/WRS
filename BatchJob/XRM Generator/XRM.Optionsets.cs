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
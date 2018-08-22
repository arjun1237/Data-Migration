using FFX.Shared.Logic.Mongo;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Transfer2Mongo
{
    class Program2
    {
        public void insertCustomerData()
        {
            // serialize the entire XML data from file
            Customers result = new Customers();  // PN: this Customer model different from the Mongo.customer. 
                                                 // Since XML model is slightly different, separate model was required  
            XmlSerializer serializer = new XmlSerializer(typeof(Customers));
            using (var file = File.OpenText(@"C:\Users\Arjun.Prasad\Desktop\Cust3.xml"))
            {
                try
                {
                    result = (Customers)serializer.Deserialize(file);
                }
                catch (Exception e)
                {
                    var ab = e.Message;
                }
            }
            
            List<Mongo.Customer> Customers = new List<Mongo.Customer>();  // Customer model from Mongo

            foreach (var Cust in result.CustomersList)
            {
                Mongo.Customer.Customer customer = new Mongo.Customer.Customer();

                customer.CustID = Cust.CustID.Trim();
                customer.Ref = Cust.Ref.Trim();
                customer.Company = Cust.Company.Trim();
                
                // transfer account info to Model.Customer.Account
                var acc2 = Cust.Account;
                if (acc2 != null)
                {
                    Mongo.Customer.Account acc = new Mongo.Customer.Account();
                    acc.AccountType = acc2.Type.Trim();
                    acc.CreditLim = acc2.CreditLim != null ? Convert.ToDecimal(acc2.CreditLim.Trim()) : 0;
                    acc.Credit = acc2.Credit != null ? Convert.ToDecimal(acc2.Credit.Trim()) : 0;
                    acc.OnStop = acc2.OnStop != null ? Convert.ToBoolean(acc2.OnStop) : false;
                    acc.Disc = acc2.Disc != null ? Int32.Parse(acc2.Disc.Trim()) : 0;
                    acc.DisplayExVAT = acc2.DisplayExVAT != null ? Convert.ToBoolean(acc2.DisplayExVAT) : false;
                    if (acc.VAT != null)
                    {
                        Mongo.Customer.VAT vat = new Mongo.Customer.VAT();
                        vat.CountryCode = acc.VAT.CountryCode.Trim();
                        vat.RegNo = acc.VAT.RegNo.Trim();
                    }
                    customer.Account = acc;
                }
                // transfer the entire customer data to Model.Customer
                Customers.Add(customer);
            }

            var custColl = MongoDbHelper.GetCollection<Mongo.Customer.Customer>("db", "cust");
            
            // insert customer data into Mongo
            custColl.InsertMany(Customers);
        }
    }
}

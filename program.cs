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
                
                // transfer account info to Model.Customer.Marketing
                var mkt2 = Cust.Marketing;
                if (mkt2 != null)
                {
                    Mongo.Customer.Marketing mkt = Mongo.Customer.Marketing();
                    mkt.Email = mkt2.OptIns.Email.Trim();
                    mkt.OptIn = mkt2.OptIns.OptIn != null ? Convert.ToBoolean(mkt2.OptIns.OptIn) : false;
                    if (DateTime.TryParse(mkt2.OptIns.OptInDate, new CultureInfo("en-GB"), DateTimeStyles.AssumeLocal, out DateTime dt))
                    {
                        mkt.OptInDate = dt;
                    }
                    customer.Marketing = mkt;
                }
                
                // transfer account info to Model.Customer.Associated
                if (Cust.AssociationParties != null)
                {
                    List<Mongo.Customer.Associated> aps = new List<Mongo.Customer.Associated>();
                    foreach (var asP in Cust.AssociationParties)
                    {
                        Mongo.Customer.Associated ap = new Mongo.Customer.Associated();
                        ap.Type = asP.Type.Trim();
                        ap.Value = asP.Value.Trim();
                        aps.Add(ap);
                    }
                    customer.Associated = aps;
                }
                
                // transfer account info to Model.Customer.Addresses
                if (Cust.Addresses != null)
                {
                    List<Mongo.Customer.CustomerAddress> css = new List<Mongo.Customer.CustomerAddress>();
                    foreach (var cs2 in Cust.Addresses)
                    {
                        Mongo.Customer.CustomerAddress cs = new Mongo.Customer.CustomerAddress();
                        cs.AddressID = cs2.AddressID.Trim();
                        cs.AddressType = cs2.AddressType.Trim();
                        cs.AddLine1 = cs2.AddLine1.Trim();
                        cs.AddLine2 = cs2.AddLine2.Trim();
                        try
                        {
                            cs.DefaultAddress = Convert.ToBoolean(cs2.DefaultAddress);
                        }
                        catch
                        {
                            cs.DefaultAddress = false;
                        }
                        cs.Company = cs2.Company.Trim();
                        cs.Town = cs2.Town == null ? "" : cs2.Town.Trim();
                        cs.Postcode = cs2.Postcode.Trim();
                        cs.Country = cs2.Country.Trim();
                        css.Add(cs);
                    }
                    customer.Addresses = css;
                }
                
                // transfer account info to Model.Customer.Contacts
                if (Cust.Contacts != null)
                {
                    List<Mongo.Customer.Contact> cons = new List<Mongo.Customer.Contact>();
                    foreach (var con2 in Cust.Contacts)
                    {
                        Mongo.Customer.Contact con = new Mongo.Customer.Contact();
                        con.ContactID = con2.ContactID.Trim();
                        try
                        {
                            con.Default = Convert.ToBoolean(con2.Default);
                        }
                        catch
                        {
                            con.Default = false;
                        }
                        con.FullName = con2.FullName.Trim();
                        con.ContactPosition = con2.ContactPosition.Trim();

                        var Phones = con2.Phones;
                        for (int i = 0; i < Phones.Count; i++)
                        {
                            Phones[i] = Phones[i].Trim();
                        }

                        con.Phones = Phones;
                        cons.Add(con);
                    }
                    customer.Contacts = cons;
                }

                // transfer the entire customer data to Model.Customer
                Customers.Add(customer);
            }

            var custColl = MongoDbHelper.GetCollection<Mongo.Customer.Customer>("db", "cust");
            
            // insert customer data into Mongo
            custColl.InsertMany(Customers);
        }
    }

    // model for XML Serializing purpose only (not to confuse with Mongo Models)

    [XmlRoot("Customers")]
    public class Customers
    {
        [XmlElement("Customer")]
        public List<Customer> CustomersList { get; set; }
    }

    public class Customer
    {
        [XmlElement("CustID")]
        public string CustID { get; set; }

        [XmlElement("Ref")]
        public string Ref { get; set; }

        [XmlElement("Company")]
        public string Company { get; set; }

        [XmlElement("Account")]
        public Account Account { get; set; }

        [XmlElement("Marketing")]
        public Marketing Marketing { get; set; }

        [XmlArray("Associated")]
        [XmlArrayItem("Associated", typeof(Associated))]
        public List<Associated> AssociationParties { get; set; }

        [XmlArray("Addresses")]
        [XmlArrayItem("Address", typeof(CustomerAddress))]
        public List<CustomerAddress> Addresses { get; set; }

        [XmlArray("Contacts")]
        [XmlArrayItem("Contact", typeof(Contact))]
        public List<Contact> Contacts { get; set; }
    }

    public class Account
    {
        [XmlElement("Type")]
        public string Type { get; set; } //cash or credit

        [XmlElement("CreditLim")]
        public string CreditLim { get; set; }

        [XmlElement("Credit")]
        public string Credit { get; set; }

        [XmlElement("OnStop")]
        public string OnStop { get; set; }

        [XmlElement("Disc")]
        public string Disc { get; set; }

        [XmlElement("DisplayExVAT")]
        public string DisplayExVAT { get; set; }

        [XmlElement("VAT")]
        public VAT VAT { get; set; }
    }

    public class VAT
    {
        [XmlElement("CountryCode")]
        public string CountryCode { get; set; }

        [XmlElement("RegNo")]
        public string RegNo { get; set; }
    }

    public class Marketing
    {
        [XmlElement("OptIns")]
        public OptIns OptIns { get; set; }
    }

    public class OptIns
    {
        [XmlElement("Email")]
        public string Email { get; set; }

        [XmlElement("OptIn")]
        public string OptIn { get; set; }

        [XmlElement("OptInDate")]
        public string OptInDate { get; set; }
    }

    public class Associated
    {
        [XmlElement("Type")]
        public string Type { get; set; }

        [XmlElement("Value")]
        public string Value { get; set; }
    }

    public class CustomerAddress
    {
        [XmlElement("AddressID")]
        public string AddressID { get; set; }

        [XmlElement("Type")]
        public string AddressType { get; set; }

        [XmlElement("Default")]
        public string DefaultAddress { get; set; }

        [XmlElement("Company")]
        public string Company { get; set; }

        [XmlElement("AddLine1")]
        public string AddLine1 { get; set; }

        [XmlElement("AddLine2")]
        public string AddLine2 { get; set; }

        [XmlElement("Town")]
        public string Town { get; set; }

        [XmlElement("Postcode")]
        public string Postcode { get; set; }

        [XmlElement("Country")]
        public string Country { get; set; }
    }

    public class Contact
    {
        [XmlElement("ContactID")]
        public string ContactID { get; set; }

        [XmlElement("Default")]
        public string Default { get; set; }

        [XmlElement("FullName")]
        public string FullName { get; set; }

        [XmlElement("Position")]
        public string ContactPosition { get; set; }

        [XmlArray("Phones")]
        [XmlArrayItem("Phone", typeof(string))]
        public List<string> Phones { get; set; }
    }
}

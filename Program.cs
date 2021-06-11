using System;
using Newtonsoft.Json;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Collections.Generic;
using System.Text.Encodings;
using System.Text;
using System.Xml.Serialization;
using System.Xml;

namespace pocitani_zprav_fb
{
    public struct Photo { }
    public struct Ucastnik
    {
        public string Jmeno { get; set; }
        public List<Message> VsechnyZpravy { get; set; }
        public int PocetFotek { get; set; }
        public List<string> VsechnySlova { get; set; }
    }

    public class Message
    {
        public string Sender_name { get; set; }
        public string Content { get; set; }
        public long Timestamp_ms { get; set; }
        public Photo[] Photos { get; set; }
        public Message() { }
    }

    static class Dekoder
    {
        static string PovoleneZnakyProZpravy = "aábcčdďeéěfghiíjklmnňoópqrřsštťuúůvwxyýzž" +
            "AÁBCČDĎEÉĚFGHIÍJKLMNŇOÓQPRŘSŠTŤUÚŮVWXYÝZŽ#1234567890()-: *,.!?@&{}+;%/´";
        static public string Dekoduj(string text)
        { //ISO-8859-1
            try
            {
                Encoding spravneKodovani = Encoding.GetEncoding("ISO-8859-1");
                var odescapovanyText = System.Text.RegularExpressions.Regex.Unescape(text);
                string s = Encoding.UTF8.GetString(spravneKodovani.GetBytes(odescapovanyText));
                foreach (char c in s)
                {
                    if (!PovoleneZnakyProZpravy.Contains(c))
                        s = s.Replace(c, '\0');
                }
                return s;
            }
            catch
            {
                return text;
            }
        }
    }

    class Statistic
    {
        public List<Ucastnik> VygenerujStatistiky(int indexSouboru, string cesta)
        {
            string json = "";
            string aktualniSoubor = string.Join(null, cesta.SkipLast(6));
            aktualniSoubor += indexSouboru;
            aktualniSoubor += ".json";
            using (StreamReader reader = new(aktualniSoubor))
            {
                json = reader.ReadToEnd();
            }
            JObject messenger = JObject.Parse(json);

            //účastníci
            IList<JToken> jtokenUcastnici = messenger["participants"].Children().ToList(); //vybrání účastníků
            List<Ucastnik> ucastniciChatu = new();
            foreach (JToken ucastnik in jtokenUcastnici)
            {
                ucastniciChatu.Add(new() { Jmeno = Dekoder.Dekoduj(ucastnik["name"].ToString()), VsechnySlova = new(), VsechnyZpravy = new() });
            }

            IList<JToken> jtokenZpravy = messenger["messages"].Children().ToList(); //vybrání zpráv
            IList<Message> zpravy = new List<Message>();
            foreach (JToken jtoken in jtokenZpravy)
            {
                if (jtoken["content"] != null || jtoken["photos"] != null)
                {
                    Message message = jtoken.ToObject<Message>();
                    zpravy.Add(message);
                }
            }
            char[] splittingChars = { ' ', '.', ',', '?', '!' };
            //slova ve všech zprávách, list všech slov
            foreach (Message zprava in zpravy) //fotky se neevidují jako zprávy
            {
                if (ucastniciChatu.All(u => u.Jmeno != Dekoder.Dekoduj(zprava.Sender_name)))
                { //přidání nesoučasného účastníka
                    ucastniciChatu.Add(new Ucastnik() { Jmeno = Dekoder.Dekoduj(zprava.Sender_name), VsechnySlova = new(), VsechnyZpravy = new() });
                }
                Ucastnik autor = ucastniciChatu.Find(u => u.Jmeno == Dekoder.Dekoduj(zprava.Sender_name));

                if (zprava.Content != null)
                { //zpráva
                    string[] slova = zprava.Content.Split(splittingChars, StringSplitOptions.RemoveEmptyEntries);
                    autor.VsechnySlova.AddRange(slova);
                    autor.VsechnyZpravy.Add(zprava);
                }
                else //fotka
                    foreach (var item in zprava.Photos)
                    {
                        autor.PocetFotek++;
                    }
                ucastniciChatu[ucastniciChatu.IndexOf(ucastniciChatu.Find(u => u.Jmeno == Dekoder.Dekoduj(zprava.Sender_name)))] = autor;
            }
            return ucastniciChatu;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Messenger statistics by 23laky");
            Console.WriteLine("Zadejte cestu k souboru message_1.json" +
                @"(např. C:\Users\Laky\message_1.json):");

            //string cestaKSouboru = Console.ReadLine().Trim();
            string cestaKSouboru = @"C:\Users\Lukáš\Desktop\kdp\message_1.json";
            while (!File.Exists(cestaKSouboru))
            {
                Console.WriteLine("Uvedený soubor neexistuje, zkuste to znovu:");
                cestaKSouboru = Console.ReadLine();
            }
            Console.Write("Zadejte počet souborů: ");
            int pocetSouboru;
            while (!int.TryParse(Console.ReadLine(), out pocetSouboru))
            {
                Console.Write("špatně zadán počet souborů, zkuste to znovu:");
            }
            Statistic Stats = new();
            List<Ucastnik> ucastnici = new();
            for (int i = 1; i <= pocetSouboru; i++)
            {
                List<Ucastnik> dalsiUcastnici = Stats.VygenerujStatistiky(i, cestaKSouboru);
                foreach (Ucastnik ucastnik in dalsiUcastnici)
                {
                    if (ucastnici.Exists(u => u.Jmeno == ucastnik.Jmeno)) //pokud existuje
                    {
                        Ucastnik puvodniUcastnik = ucastnici.Find(u => u.Jmeno == ucastnik.Jmeno);
                        puvodniUcastnik.VsechnySlova.AddRange(ucastnik.VsechnySlova);
                        puvodniUcastnik.VsechnyZpravy.AddRange(ucastnik.VsechnyZpravy);
                        puvodniUcastnik.PocetFotek += ucastnik.PocetFotek;
                        ucastnici[ucastnici.IndexOf(ucastnici.Find(u => u.Jmeno == ucastnik.Jmeno))] = puvodniUcastnik;
                    }
                    else //přidáme nového pokud zatím neexistoval
                        ucastnici.Add(ucastnik);
                }
                Console.Clear();
                Console.Write($"Zpracován soubor č. {i}/{pocetSouboru}");
            }
            Vypis(ucastnici);

            Console.ReadKey();
        }
        static void Vypis(List<Ucastnik> ucastnici)
        {
            Console.WriteLine($"\nCelkem se v tomto chatu napsalo {VypisCelkemZpravVKonverzaci(ucastnici)} zpráv.");

            Console.WriteLine($"Celkem se v tomto chatu poslalo {VypisCelkemFotekVKonverzaci(ucastnici)} fotek.\n");

            var dotaz = from u in ucastnici //seřazení účastníků podle jejich příspěvků
                        orderby u.VsechnyZpravy.Count descending
                        select u;
            ucastnici = dotaz.ToList();
            foreach (Ucastnik ucastnik in ucastnici)
            {
                if (ucastnik.VsechnyZpravy.Count > 0)
                {
                    Console.WriteLine("______________________________________");
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine(ucastnik.Jmeno);
                    Console.ResetColor();
                    Console.Write($"Napsal {ucastnik.VsechnyZpravy.Count} zpráv a celkem {ucastnik.VsechnySlova.Count} slov,");
                    Console.WriteLine($"t.j. v průměru {Math.Round((double)ucastnik.VsechnySlova.Count / (double)ucastnik.VsechnyZpravy.Count, 2)} slov ve zprávě.");
                    Console.WriteLine($"Poslal celkem {ucastnik.PocetFotek} fotek.\n");


                    Dictionary<string, int> ucastnikuvSlovnik = VygenerujZebricek(ucastnik.VsechnySlova);

                    //VypisNejpouzivanejsiSlova(ucastnikuvSlovnik);
                    //VypisNejdelsiSlovo(ucastnikuvSlovnik);
                    //VypisUnikatniSlova(ucastnikuvSlovnik);

                    //VypisPrvniAPosledniZpravu(ucastnik);
                    //VypisNejdelsiZpravu(ucastnik);
                    //VypisNejaktivnejsiRoky(ucastnik);
                    //VypisNeajktivnejsiDen(ucastnik);
                    VypisNahodnouZpravu(ucastnik); //tato metoda pouze zapisuje do xml, ne do konzole
                }
            }
        }
        static void VypisNahodnouZpravu(Ucastnik participant)
        {
            Random random = new();
            List<Message> listOfRandomMessages = new();
            var query = from m in participant.VsechnyZpravy
                        where m.Content.Length > 50 //minimální délka zprávy ve znacích
                        select m;
            for (int i = 0; i < 5; i++) //počet náhodných zpráv
            {
                int numberOfMessage = random.Next(query.Count());
                listOfRandomMessages.Add(query.ToList()[numberOfMessage]);
            }

            XmlSerializer serializer = new(listOfRandomMessages.GetType());
            using (StreamWriter writer = new(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                $@"23lakySoft\RandomMessages-{participant.Jmeno}.xml")))
            {
                serializer.Serialize(writer, listOfRandomMessages);
            }
        }

        static int VypisCelkemFotekVKonverzaci(List<Ucastnik> participants)
        {
            var query = (from p in participants
                         select p.PocetFotek).Sum();
            return query;
        }

        static void VypisNeajktivnejsiDen(Ucastnik ucastnik)
        {
            var dotaz = (from z in ucastnik.VsechnyZpravy
                         group z by UnixTimeStampToDateTime(z.Timestamp_ms).Date into Dny
                         orderby Dny.Count() descending
                         select Dny).Take(3);
            Console.WriteLine("Toto jsou 3 nejaktivnější dny:");
            foreach (var den in dotaz)
            {
                Console.WriteLine($"{den.Key.ToShortDateString()} - {den.Count()} zpráv");
            }
        }
        static void VypisNejaktivnejsiRoky(Ucastnik ucastnik)
        {
            var dotaz = from z in ucastnik.VsechnyZpravy
                        group z by UnixTimeStampToDateTime(z.Timestamp_ms).Year into Roky
                        orderby Roky.Count() descending
                        select Roky;
            Console.WriteLine("Rozdělení podle let:");
            foreach (var rok in dotaz)
            {
                Console.WriteLine($"{rok.Key} - {rok.Count()} zpráv");
            }
        }

        static void VypisNejdelsiZpravu(Ucastnik ucastnik)
        {
            var dotaz = (from z in ucastnik.VsechnyZpravy
                         where !Dekoder.Dekoduj(z.Content).StartsWith("https:")
                         orderby z.Content.Length descending
                         select z).Take(3);
            Console.WriteLine("\nToto jsou jeho 3 nejdelší zprávy:");
            if (dotaz.Count() == 3)
            {
                foreach (Message zprava in dotaz)
                {
                    Console.WriteLine($"{UnixTimeStampToDateTime((zprava as Message).Timestamp_ms).ToShortDateString()}: {Dekoder.Dekoduj((zprava as Message).Content)}\n");
                }
            }
        }

        static void VypisPrvniAPosledniZpravu(Ucastnik ucastnik)
        {
            var dotaz = from z in ucastnik.VsechnyZpravy
                        orderby z.Timestamp_ms
                        select z;
            if (dotaz.Count() > 0)
            {
                Console.WriteLine("\nToto je první zpráva, kterou do chatu napsal:");
                Console.WriteLine($"{UnixTimeStampToDateTime((dotaz.First() as Message).Timestamp_ms).ToShortDateString()}: {Dekoder.Dekoduj((dotaz.First() as Message).Content)}");
                Console.WriteLine("\nToto je poslední zpráva, kterou do chatu napsal:");
                Console.WriteLine($"{UnixTimeStampToDateTime((dotaz.Last() as Message).Timestamp_ms).ToShortDateString()}: {Dekoder.Dekoduj((dotaz.Last() as Message).Content)}");
            }
        }

        static void VypisNejdelsiSlovo(Dictionary<string, int> slovnik)
        {
            var dotaz = (from s in slovnik
                         where Dekoder.Dekoduj(s.Key).All(c => char.IsLetterOrDigit(c) || c == '#')
                         orderby s.Key.Length descending
                         select s.Key).Take(3);
            Console.WriteLine($"\nToto jsou 3 nejdelší slova, které použil:");
            foreach (var slovo in dotaz)
            {
                Console.WriteLine(Dekoder.Dekoduj(slovo));
            }
        }

        static void VypisUnikatniSlova(Dictionary<string, int> slovnik)
        {
            var dotaz = from s in slovnik
                        where s.Value == 1
                        select s;
            Console.WriteLine($"\nCelkem použil {dotaz.Count()} unikátních slov.");
        }

        static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddMilliseconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }

        static int VypisCelkemZpravVKonverzaci(List<Ucastnik> list)
        {
            var dotaz = (from u in list
                         select u.VsechnyZpravy.Count).Sum();
            return dotaz;
        }

        static void VypisNejpouzivanejsiSlova(Dictionary<string, int> zebricek)
        {
            var dotaz = (from par in zebricek
                         where Dekoder.Dekoduj(par.Key).Length > 3 && Dekoder.Dekoduj(par.Key).All(c => char.IsLetterOrDigit(c)) && !GenerickeSlova.Contains(Dekoder.Dekoduj(par.Key))
                         orderby par.Value descending
                         select par).Take(20);

            Console.WriteLine("20 nejpoužívanějších slov v tomto chatu je:");
            int poradi = 1;
            foreach (var slovo in dotaz)
            {
                Console.Write($"{poradi}.\t");
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write($"{Dekoder.Dekoduj(slovo.Key)}".PadRight(18));
                Console.ResetColor();
                Console.WriteLine($"použito {slovo.Value}x");
                poradi++;
            }
        }

        static Dictionary<string, int> VygenerujZebricek(List<string> seznamSlov)
        {
            Dictionary<string, int> zebricek = new();
            foreach (string slovo in seznamSlov)
            {
                if (zebricek.TryGetValue(slovo, out int vyskyt)) //pokud žebříček obsahuje zmíněné slovo, zvýší jeho výskyt                                        //výskyt++
                    zebricek[slovo] = ++vyskyt;
                else                                             //jinak ho zalistuje a přidá mu hodnotu výskytu 1x
                    zebricek.Add(slovo, 1);
            }
            return zebricek;
        }

        static string[] GenerickeSlova = { "kdyz", "když", "taky", "bych", "proste", "prostě", "ještě", "jeste",
            "nebo", "jako", "uplne", "úplně", "bude", "jsou", "jsem", "třeba", "treba", "bejt", "nekdo", "toho",
            "fakt", "nekdo", "někdo", "budu", "jestli", "protože", "vsichni", "všichni","jenom","něco","neco","není",
            "takže","stejně","jsme","těch","spíš","snad","vubec","bylo","teda","tohle","kdyby","neni","budou", "furt",
            "někdy","posílá","přílohu","nebylo","tenhle","tady","tomu","takhle","podle","všechno","tvoje","nebude",
            "nikdo","dnes","dneska","jste","skoro","tebe","jeho","stále","hodně","nějaký","nejaky","dost","aspon","porad",
            "který","takový","vůbec","kvůli","kvuli","hned","prave","zase","stejne","kazdej","každej","udělal","tohodle",
            "pokud","přesně","vždycky","aspoň","nejak","byla","jinak","nikdy","budem","takze","vsechny","hlavne","vsechno",
            "uplně","možná","mnou","nějak","opět","alespoň","mohl","nejsou","máme","protoze","youtube","Jako","Taky","Fakt",
            "Jsem","Posíláte","Proste","Třeba","jono","Jono","Jsem","Nebo","Když","Takze","Protože","Vždyť","Není","mezi",
            "facebook","docela","chtel","chci","myslim","Skupině","fotku","kolik","mame","prej","todle","nejde","proc",
            "dobry","cely","tesim","nejlepsi","nejvic"};
    }
}

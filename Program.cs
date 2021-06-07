using System;
using Newtonsoft.Json;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Collections.Generic;
using System.Text.Encodings;
using System.Text;

namespace pocitani_zprav_fb
{
    struct Ucastnik
    {
        public string Jmeno { get; set; }
        public List<Message> VsechnyZpravy { get; set; }
        public List<string> VsechnySlova { get; set; }
    }
    class Program
    {
        static string PovoleneZnakyProZpravy = "aábcčdďeéěfghiíjklmnňoópqrřsštťuúůvwxyýzžAÁBCČDĎEÉĚFGHIÍJKLMNŇOÓQPRŘSŠTŤUÚŮVWXYÝZŽ#1234567890()-: *,.!?@&{}+;%/";
        static string PovoleneZnaky = "aábcčdďeéěfghiíjklmnňoópqrřsštťuúůvwxyýzžAÁBCČDĎEÉĚFGHIÍJKLMNŇOÓQPRŘSŠTŤUÚŮVWXYÝZŽ#";
        static string[] GenerickeSlova = { "kdyz", "když", "taky", "bych", "proste", "prostě", "ještě", "jeste",
            "nebo", "jako", "uplne", "úplně", "bude", "jsou", "jsem", "třeba", "treba", "bejt", "nekdo", "toho",
            "fakt", "nekdo", "někdo", "budu", "jestli", "protože", "vsichni", "všichni","jenom","něco","neco","není",
            "takže","stejně","jsme","těch","spíš","snad","vubec","bylo","teda","tohle","kdyby","neni","budou", "furt",
            "někdy","posílá","přílohu","nebylo","tenhle","tady","tomu","takhle","podle","všechno","tvoje","nebude",
            "nikdo","dnes","dneska","jste","skoro","tebe","jeho","stále","hodně","nějaký","nejaky","dost","aspon","porad",
            "který","takový","vůbec","kvůli","kvuli","hned","prave","zase","stejne","kazdej","každej","udělal","tohodle","pokud",
            "přesně","vždycky","aspoň","nejak","byla","jinak","nikdy","budem","takze","vsechny","hlavne","vsechno","uplně",
            "možná","mnou","nějak","opět","alespoň","mohl","nejsou","máme","protoze","youtube","Jako","Taky","Fakt","Jsem","Posíláte",
            "Proste","Třeba","jono","Jono","Jsem","Nebo","Když","Takze","Protože","Vždyť","Není","mezi","facebook","docela","chtel","chci",
            "myslim","Skupině","fotku","kolik","mame","prej","todle","nejde","proc","dobry","cely","tesim","nejlepsi","nejvic"};
        static int VelikostZebricku = 3;
        static void Main(string[] args)
        {
            Console.WriteLine("Messenger statistics by 23laky");
            Console.Write("Zadejte počet souborů:");
            int pocetSouboru = int.Parse(Console.ReadLine());
            List<Ucastnik> ucastnici = VygenerujStatistiky(1);
            if (pocetSouboru > 1)
            {
                for (int i = 2; i <= pocetSouboru; i++)
                {
                    List<Ucastnik> dalsiUcastnici = VygenerujStatistiky(i);
                    foreach (Ucastnik ucastnik in dalsiUcastnici)
                    {
                        if (ucastnici.Exists(u => u.Jmeno == ucastnik.Jmeno)) //pokud existuje
                        {
                            Ucastnik puvodniUcastnik = ucastnici.Find(u => u.Jmeno == ucastnik.Jmeno);
                            puvodniUcastnik.VsechnySlova.AddRange(ucastnik.VsechnySlova);
                            puvodniUcastnik.VsechnyZpravy.AddRange(ucastnik.VsechnyZpravy);
                            ucastnici[ucastnici.IndexOf(ucastnici.Find(u => u.Jmeno == ucastnik.Jmeno))] = puvodniUcastnik;
                        }
                        else //přidáme nového pokud zatím neexistoval
                        {
                            ucastnici.Add(ucastnik);
                        }
                    }
                    Console.Clear();
                    Console.Write($"Zpracován soubor č. {i}/{pocetSouboru}");
                }
            }
            Vypis(ucastnici);
            Console.ReadKey();
        }
        static void Vypis(List<Ucastnik> ucastnici)
        {
            Console.WriteLine($"\nCelkem se v tomto chatu napsalo {VsechnyZpravy(ucastnici)} zpráv\n");
            var dotaz = from u in ucastnici
                        orderby u.VsechnyZpravy.Count descending
                        select u;
            ucastnici = dotaz.ToList();
            foreach (Ucastnik ucastnik in ucastnici)
            {
                if (ucastnik.VsechnyZpravy.Count > 0)
                {
                    VypisCelkemSlovAZprav(ucastnik);

                    Dictionary<string, int> ucastnikuvSlovnik = VygenerujZebricek(ucastnik.VsechnySlova);
                    VypisNejpouzivanejsiSlova(ucastnikuvSlovnik);
                    VypisNejdelsiSlovo(ucastnikuvSlovnik);
                    VypisUnikatniSlova(ucastnikuvSlovnik);

                    VypisPrvniAPosledniZpravu(ucastnik);
                    VypisNejdelsiZpravu(ucastnik);
                    VypisNejaktivnejsiRoky(ucastnik);
                    VypisNeajktivnejsiDen(ucastnik);
                }
            }
        }
        static void VypisCelkemSlovAZprav(Ucastnik ucastnik)
        {
            Console.WriteLine("______________________________________");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(ucastnik.Jmeno);
            Console.ResetColor();
            Console.Write($"Napsal {ucastnik.VsechnyZpravy.Count} zpráv a celkem {ucastnik.VsechnySlova.Count} slov,");
            Console.WriteLine($"t.j. v průměru {Math.Round((double)ucastnik.VsechnySlova.Count / (double)ucastnik.VsechnyZpravy.Count, 2)} slov ve zprávě.\n");
        }
        static void VypisNeajktivnejsiDen(Ucastnik ucastnik)
        {
            var dotaz = (from z in ucastnik.VsechnyZpravy
                         group z by UnixTimeStampToDateTime(z.timestamp_ms).Date into Dny
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
                        group z by UnixTimeStampToDateTime(z.timestamp_ms).Year into Roky
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
                         where Dekoduj(z.content).All(c => PovoleneZnakyProZpravy.Contains(c))
                         orderby z.content.Length descending
                         select z).Take(3);
            Console.WriteLine("\nToto jsou jeho 3 nejdelší zprávy:");
            if (dotaz.Count() == 3)
            {
                foreach (Message zprava in dotaz)
                {
                    Console.WriteLine($"{UnixTimeStampToDateTime((zprava as Message).timestamp_ms).ToShortDateString()}: {Dekoduj((zprava as Message).content)}\n");
                }
            }
        }
        static void VypisPrvniAPosledniZpravu(Ucastnik ucastnik)
        {
            var dotaz = from z in ucastnik.VsechnyZpravy
                        where Dekoduj(z.content).All(c => PovoleneZnakyProZpravy.Contains(c))
                        orderby z.timestamp_ms
                        select z;
            if (dotaz.Count() > 0)
            {
                Console.WriteLine("\nToto je první zpráva, kterou do chatu napsal:");
                Console.WriteLine($"{UnixTimeStampToDateTime((dotaz.First() as Message).timestamp_ms).ToShortDateString()}: {Dekoduj((dotaz.First() as Message).content)}");
                Console.WriteLine("\nToto je poslední zpráva, kterou do chatu napsal:");
                Console.WriteLine($"{UnixTimeStampToDateTime((dotaz.Last() as Message).timestamp_ms).ToShortDateString()}: {Dekoduj((dotaz.Last() as Message).content)}");
            }
        }
        static void VypisNejdelsiSlovo(Dictionary<string, int> slovnik)
        {
            var dotaz = (from s in slovnik
                         where Dekoduj(s.Key).All(c => PovoleneZnaky.Contains(c))
                         orderby s.Key.Length descending
                         select s.Key).Take(3);
            Console.WriteLine($"\nToto jsou 3 nejdelší slova, které použil:");
            foreach (var slovo in dotaz)
            {
                Console.WriteLine(Dekoduj(slovo));
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
        static int VsechnyZpravy(List<Ucastnik> list)
        {
            var dotaz = (from u in list
                         select u.VsechnyZpravy.Count).Sum();
            return dotaz;
        }
        static void VypisNejpouzivanejsiSlova(Dictionary<string, int> zebricek)
        {
            var dotaz = (from par in zebricek
                         where Dekoduj(par.Key).Length > 3 && Dekoduj(par.Key).All(c => PovoleneZnaky.Contains(c)) && !GenerickeSlova.Contains(Dekoduj(par.Key))
                         orderby par.Value descending
                         select par).Take(20); //vybereme pouze 10 nejpoužívanějších

            Console.WriteLine("20 nejpoužívanějších slov v tomto chatu je:");
            int poradi = 1;
            foreach (var slovo in dotaz)
            {
                Console.Write($"{poradi}.\t");
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write($"{Dekoduj(slovo.Key)}".PadRight(18));
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
                if (zebricek.TryGetValue(slovo, out int vyskyt)) //pokud žebříček obsahuje zmíněné slovo, zvýší jeho
                {                                                //výskyt++
                    zebricek[slovo] = ++vyskyt;
                }
                else                                             //jinak ho zalistuje a přidá mu hodnotu výskytu 1x
                {
                    zebricek.Add(slovo, 1);
                }
            }
            return zebricek;
        }
        static string Dekoduj(string text)
        { //ISO-8859-1
            try
            {
                Encoding spravneKodovani = Encoding.GetEncoding("ISO-8859-1");
                var odescapovanyText = System.Text.RegularExpressions.Regex.Unescape(text);
                return Encoding.UTF8.GetString(spravneKodovani.GetBytes(odescapovanyText));
            }
            catch (Exception)
            {
                return text;
            }

        }
        static List<Ucastnik> VygenerujStatistiky(int indexSouboru)
        {
            string json = "";
            using (StreamReader reader = new(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), @$"kdp\message_{indexSouboru}.json")))
            {
                json = reader.ReadToEnd();
            }
            JObject messenger = JObject.Parse(json);

            //účastníci
            IList<JToken> jtokenUcastnici = messenger["participants"].Children().ToList(); //vybrání účastníků
            List<Ucastnik> ucastniciChatu = new();
            foreach (JToken ucastnik in jtokenUcastnici)
            {
                ucastniciChatu.Add(new() { Jmeno = Dekoduj(ucastnik["name"].ToString()), VsechnySlova = new(), VsechnyZpravy = new() });
            }


            IList<JToken> jtokenZpravy = messenger["messages"].Children().ToList(); //vybrání zpráv
            IList<Message> zpravy = new List<Message>();
            foreach (JToken jtoken in jtokenZpravy)
            {
                if (jtoken["content"] != null)
                {
                    Message message = jtoken.ToObject<Message>();
                    zpravy.Add(message);
                }
            }
            char[] splittingChars = { ' ', '.', ',', '?', '!' };
            //slova ve všech zprávách -> list všech slov
            foreach (Message zprava in zpravy)
            {
                string[] slova = zprava.content.Split(splittingChars, StringSplitOptions.RemoveEmptyEntries);
                //slovaVeVsechZpravach.AddRange(slova);

                if (ucastniciChatu.All(u => u.Jmeno != Dekoduj(zprava.sender_name)))
                { //přidání nesoučasného účastníka
                    ucastniciChatu.Add(new Ucastnik() { Jmeno = Dekoduj(zprava.sender_name), VsechnySlova = new(), VsechnyZpravy = new() });
                }

                Ucastnik autor = ucastniciChatu.Find(u => u.Jmeno == Dekoduj(zprava.sender_name));
                autor.VsechnySlova.AddRange(slova);
                autor.VsechnyZpravy.Add(zprava);
                ucastniciChatu[ucastniciChatu.IndexOf(ucastniciChatu.Find(u => u.Jmeno == Dekoduj(zprava.sender_name)))] = autor;
            }
            return ucastniciChatu;
        }
    }
}

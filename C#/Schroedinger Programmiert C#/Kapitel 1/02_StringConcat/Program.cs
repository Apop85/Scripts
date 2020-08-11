using System;

// Namespace definieren
namespace _02_StringConcat
{
    // Klassendefinition
    class Program
    {
        // Main Funktion
        static void Main(string[] args)
        {
            Console.WriteLine("Bitte deinen Namen eingeben:");
            var name = Console.ReadLine();
            Console.WriteLine("Hello " + name);
            Console.ReadKey();
        }
    }
}

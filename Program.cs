using System.Text;
using WorkstationJobSimulator.EventPhysic;
using WorkstationJobSimulator.Events;



// Модель станції
using WorkstationJobSimulator.Models.wsModels;

// Генератор подій (з твоїм EventChance, RollNextInterval тощо)

// Підключаємо простір імен, де живе фізика подій

namespace WorkstationJobSimulator;

public class Program
{
    public static void Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        // 1) Створюємо генератор подій (в ньому зберігається рандомність, шанси подій тощо)
        var generator = new SimulationEventGenerator();

        // 2) Створюємо робочу станцію
        var workstation = new Workstation("Робоча станція №1");

        // 3) Створюємо двигун фізики,
        //    який буде знати, яку логіку застосувати для кожного типу події
        var physicsEngine = new WorkstationPhysicsEngine();

        // 4) Реєструємо майстре клас фізики, який додасть усі реалізації IEventPhysics в двигун
        PhysicsRegistry.RegisterAllEventPhysics(physicsEngine);


        Console.WriteLine("Натисніть будь-яку клавішу, щоб почати симуляцію...");
        Console.ReadKey();
        Console.Clear();

        const int iterations = 5;

        // Основний цикл симуляції
        for (int i = 0; i < iterations; i++)
        {
            Console.WriteLine(new string('=', 70));
            Console.WriteLine($"ІТЕРАЦІЯ #{i + 1}");

            // 5) Вираховуємо випадковий інтервал до наступної події
            var wait = generator.RollNextInterval();
            Console.WriteLine(
                $"[LOG] Очікуємо наступну подію приблизно через {wait.TotalSeconds:F0} секунд...");
            Thread.Sleep(wait);

            // 6) Генеруємо випадкову подію на основі EventChance-ваг
            var ev = generator.Generate();

            Console.WriteLine();
            Console.WriteLine($"[ENGINE] Згенеровано подію: \"{ev.EventName}\" (тривалість: {ev.Duration})");

            // 7) Замість того, щоб тягнути всю логіку в Workstation.ProcessEvent,
            //    ми віддаємо подію в двигун фізики:
            //    - Engine по типу ev знаходить правильний IEventPhysics
            //    - і застосовує фізику до workstation
            physicsEngine.ApplyPhysics(workstation, ev);

            // (Опціонально) якщо хочеш ще додатково бачити стан після події:
            Console.WriteLine();
            Console.WriteLine("[STATE] Стан станції після обробки події:");
            workstation.PrintStatus();
        }

        Console.WriteLine(new string('=', 70));
        Console.WriteLine("Симуляцію завершено. Натисніть будь-яку клавішу для виходу...");
        Console.ReadKey();
    }
}

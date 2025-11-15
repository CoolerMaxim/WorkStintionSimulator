using System.Text;
using System.Threading;
using WorkstationJobSimulator.EventPhysic;
using WorkstationJobSimulator.Events;
using WorkstationJobSimulator.Models.wsModels;

Console.OutputEncoding = Encoding.UTF8;

var generator = new SimulationEventGenerator();
var workstation = new Workstation("Робоча станція №1");
var physicsEngine = new WorkstationPhysicsEngine();
PhysicsRegistry.RegisterAllEventPhysics(physicsEngine);

Console.WriteLine("Натисніть будь-яку клавішу, щоб почати симуляцію...");
Console.ReadKey();
Console.Clear();

const int iterations = 5;

for (int i = 0; i < iterations; i++)
{
    Console.WriteLine(new string('=', 70));
    Console.WriteLine($"ІТЕРАЦІЯ #{i + 1}");

    var wait = generator.RollNextInterval();
    Console.WriteLine($"[LOG] Очікуємо наступну подію приблизно через {wait.TotalSeconds:F0} секунд...");
    Thread.Sleep(wait);

    var simulationEvent = generator.Generate();

    Console.WriteLine();
    Console.WriteLine($"[ENGINE] Згенеровано подію: \"{simulationEvent.EventName}\" (тривалість: {simulationEvent.Duration})");

    physicsEngine.ApplyPhysics(workstation, simulationEvent);
}

Console.WriteLine(new string('=', 70));
Console.WriteLine("Симуляцію завершено. Натисніть будь-яку клавішу для виходу...");
Console.ReadKey();

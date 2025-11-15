using TelemetryGenerator.Core.Configuration;
using TelemetryGenerator.Core.State;

namespace TelemetryGenerator.Core.Modules;

/// <summary>
/// Базовий інтерфейс для всіх модулів вузла.
/// </summary>
public interface IModule
{
    /// <param name="state">Поточний стан вузла.</param>
    /// <param name="config">Статична конфігурація вузла.</param>
    /// <param name="dt">Тривалість кроку моделювання.</param>
    /// <param name="rnd">Генератор випадкових чисел.</param>
    void Update(NodeState state, NodeConfig config, TimeSpan dt, Random rnd);
}

using System;

namespace WorkstationJobSimulator.Models;

/// <summary>
/// Набір параметрів симуляції з безпечними значеннями за замовчуванням.
/// </summary>
public class SimulationParameters
{
    public string WorkstationName { get; init; } = "Робоча станція №1";
    public int Iterations { get; init; } = 5;
    public int MinEventIntervalSeconds { get; init; } = 20;
    public int MaxEventIntervalSeconds { get; init; } = 40;

    /// <summary>
    /// Мінімальна пауза перед запуском симуляції, щоб користувач міг переглянути налаштування.
    /// </summary>
    public TimeSpan StartupDelay { get; init; } = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Шлях до конфігураційного файлу з вагами подій. Якщо null або файл не існує, використовуються значення з атрибутів.
    /// </summary>
    public string? EventWeightsConfigPath { get; init; } = "eventWeights.json";

    /// <summary>
    /// Детермінований seed для генератора випадкових чисел. Якщо null – використовується значення за замовчуванням.
    /// </summary>
    public int? RandomSeed { get; init; }

    public static SimulationParameters Default => new();
}

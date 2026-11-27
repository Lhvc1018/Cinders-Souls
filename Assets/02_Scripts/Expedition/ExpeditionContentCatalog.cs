using System;
using UnityEngine;
using Codice.Enemies;
namespace Codice.Expedition
{
    [CreateAssetMenu(menuName="Cinders Souls/Catálogo procedural")]
    public sealed class ExpeditionContentCatalog : ScriptableObject
    {
        [Tooltip("Vacío conserva los enemigos actuales. Añade prefabs con BasicEnemy.")]
        public BasicEnemy[] additionalEnemies = Array.Empty<BasicEnemy>();
        [Tooltip("Decoración lateral. Pivote al suelo; ancho máximo recomendado: 3 metros.")]
        public GameObject[] additionalProps = Array.Empty<GameObject>();
        [Tooltip("Opcional: reemplaza al jefe del piso 1. Vacío conserva al Inquisidor.")]
        public Codice.Bosses.BossCharacter floorOneBoss;
        [Tooltip("Opcional: reemplaza al jefe del piso 2. Vacío conserva al Ángel.")]
        public Codice.Bosses.BossCharacter floorTwoBoss;
        [Tooltip("Opcional: reemplaza al jefe del piso 3. Vacío conserva a Gurdi del Abismo.")]
        public Codice.Bosses.BossCharacter floorThreeBoss;
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CombatBalanceTests
{
    private static ScriptableObject Asset(string path) => AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
    private static IEnumerable<ScriptableObject> Assets(string folder) => AssetDatabase.FindAssets("t:ScriptableObject", new[] {folder})
        .Select(guid => Asset(AssetDatabase.GUIDToAssetPath(guid))).Where(asset => asset != null);
    private static MonoBehaviour Component(GameObject root, string name) => root.GetComponentsInChildren<MonoBehaviour>(true)
        .Single(script => script != null && script.GetType().Name == name);
    private static float Number(UnityEngine.Object obj, string field)
    {
        var property = new SerializedObject(obj).FindProperty(field);
        return property.propertyType == SerializedPropertyType.Float ? property.floatValue : property.intValue;
    }

    [Test]
    public void Weapons_OfferDistinctTradeoffs_WithoutExtremePowerCreep()
    {
        var starter = Asset("Assets/Data/Weapons/WoodStaff.asset");
        float starterDps = Number(starter, "damage") / Number(starter, "fireRate");
        var profiles = new HashSet<string>();
        var weapons = Assets("Assets/Data/Weapons").ToArray();
        foreach (var weapon in weapons)
        {
            float count = Number(weapon, "projectilesPerShot");
            float interval = Number(weapon, "fireRate");
            float damage = Number(weapon, "damage");
            float range = Number(weapon, "range");
            Assert.That(interval, Is.GreaterThanOrEqualTo(.2f), weapon.name);
            Assert.That(damage * count / interval, Is.InRange(starterDps * .9f, starterDps * 1.5f), weapon.name);
            Assert.That(Number(weapon, "damageMultiplier"), Is.EqualTo(1), "Gậy không được nhân chéo sức mạnh skill: " + weapon.name);
            Assert.That(range, Is.InRange(6f, 11f), weapon.name);
            Assert.That(Number(weapon, "projectileSpeed"), Is.GreaterThan(0), weapon.name);
            profiles.Add(damage + "/" + interval + "/" + range + "/" + count);
        }
        Assert.That(profiles.Count, Is.EqualTo(weapons.Length), "Gậy phải có khác biệt chỉ số thực tế.");
    }

    [Test]
    public void UnlockRecipes_UseReachableMaterials_AndNeverConsumeKeysOrHealing()
    {
        var sources = new HashSet<UnityEngine.Object>();
        foreach (var table in Assets("Assets/Data/LootTables").Where(a => a.GetType().Name == "LootTableData"))
        {
            var entries = new SerializedObject(table).FindProperty("entries");
            for (int i = 0; i < entries.arraySize; i++)
            {
                var entry = entries.GetArrayElementAtIndex(i);
                if (entry.FindPropertyRelative("chanceMultiplier").floatValue > 0)
                    sources.Add(entry.FindPropertyRelative("item").objectReferenceValue);
            }
        }
        foreach (var folder in new[] {"Assets/Data/Weapons", "Assets/Data/Skins", "Assets/Data/Skills"})
            foreach (var asset in Assets(folder).Where(a => new[] {"WeaponData", "PlayerSkinData", "SkillData"}.Contains(a.GetType().Name)))
            {
                var costs = new SerializedObject(asset).FindProperty("unlockItems");
                for (int i = 0; i < costs.arraySize; i++)
                {
                    var row = costs.GetArrayElementAtIndex(i);
                    var item = row.FindPropertyRelative("item").objectReferenceValue;
                    Assert.That(item, Is.Not.Null, asset.name);
                    Assert.That(sources.Contains(item), Is.True, asset.name + ": nguyên liệu không có nguồn rơi " + item.name);
                    Assert.That(row.FindPropertyRelative("amount").intValue, Is.GreaterThan(0), asset.name);
                    var data = new SerializedObject(item);
                    Assert.That(data.FindProperty("isKey").boolValue, Is.False, asset.name);
                    Assert.That(data.FindProperty("pickupEffect").enumValueIndex, Is.EqualTo(0), asset.name);
                }
            }
    }

    [TestCase("RewardChestLoot", "M_F1_GateKey")]
    [TestCase("TenguLoot", "monster_essence")]
    [TestCase("EvilWitchLoot", "monster_essence")]
    public void EssentialRewards_AreGuaranteed_ByActualLootRolls(string tableName, string material)
    {
        var table = Asset("Assets/Data/LootTables/" + tableName + ".asset");
        var item = Asset("Assets/Data/Items/" + material + ".asset");
        var roll = table.GetType().GetMethod("Roll");
        var random = new System.Random(20261106);
        for (int i = 0; i < 1000; i++)
        {
            var stacks = (IEnumerable)roll.Invoke(table, new object[] {random});
            bool found = false;
            foreach (var stack in stacks)
                found |= (UnityEngine.Object)stack.GetType().GetField("item").GetValue(stack) == item;
            Assert.That(found, Is.True, tableName + " thiếu " + material + " ở lần " + i);
        }
    }

    [TestCase("tengu_wing", "TenguLoot")]
    [TestCase("tengu_mask", "TenguLoot")]
    [TestCase("scrying_orb", "EvilWitchLoot")]
    [TestCase("light_essence", "EvilWitchLoot")]
    [TestCase("staff_fragment", "RewardChestLoot")]
    [TestCase("outfit_fragment", "RewardChestLoot")]
    [TestCase("ruby", "RewardChestLoot")]
    [TestCase("topaz", "RewardChestLoot")]
    [TestCase("emerald", "RewardChestLoot")]
    [TestCase("diamond", "RewardChestLoot")]
    [TestCase("white_dye", "RewardChestLoot")]
    public void ExclusiveItems_HaveExactlyOneLootSource(string material, string source)
    {
        var item = Asset("Assets/Data/Items/" + material + ".asset");
        Assert.That(item, Is.Not.Null);
        var found = new List<string>();
        foreach (var table in Assets("Assets/Data/LootTables").Where(a => a.GetType().Name == "LootTableData"))
        {
            var entries = new SerializedObject(table).FindProperty("entries");
            for (int i = 0; i < entries.arraySize; i++)
            {
                var entry = entries.GetArrayElementAtIndex(i);
                if (entry.FindPropertyRelative("item").objectReferenceValue == item &&
                    entry.FindPropertyRelative("chanceMultiplier").floatValue > 0)
                    found.Add(table.name);
            }
        }
        Assert.That(found, Is.EquivalentTo(new[] {source}), material + " có nguồn rơi sai hoặc bị khai báo trùng.");
    }

    [TestCase("TenguLoot", "tengu_wing", .65)]
    [TestCase("TenguLoot", "tengu_mask", .40)]
    [TestCase("EvilWitchLoot", "scrying_orb", .55)]
    [TestCase("EvilWitchLoot", "light_essence", .45)]
    public void BossExclusiveRewards_CanMiss_AndFollowConfiguredRates(string tableName, string material, double expected)
    {
        var table = Asset("Assets/Data/LootTables/" + tableName + ".asset");
        var item = Asset("Assets/Data/Items/" + material + ".asset");
        var roll = table.GetType().GetMethod("Roll");
        var random = new System.Random(20261106);
        const int samples = 10000;
        int successes = 0;
        for (int i = 0; i < samples; i++)
        {
            bool found = false;
            foreach (var stack in (IEnumerable)roll.Invoke(table, new object[] {random}))
                found |= (UnityEngine.Object)stack.GetType().GetField("item").GetValue(stack) == item;
            if (found) successes++;
        }
        Assert.That(successes, Is.InRange(1, samples - 1), "Không được biến đồ độc quyền thành phần thưởng chắc chắn.");
        Assert.That((double)successes / samples, Is.EqualTo(expected).Within(.02), material);
    }

    [Test]
    public void NormalEnemies_HaveLongPursuit_FasterWindup_AndCanBeOutrun()
    {
        foreach (var name in new[] {"skeletonWarrior", "ArcherSkeleton", "DarkKnight"})
        {
            var root = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy/" + name + ".prefab");
            var ai = Component(root, "EnemyAI");
            Assert.That(Number(ai, "detectionRadius"), Is.GreaterThanOrEqualTo(10), name);
            Assert.That(Number(ai, "loseTargetRadius"), Is.GreaterThan(Number(ai, "detectionRadius")), name);
            Assert.That(Number(ai, "moveSpeed"), Is.LessThan(5f), name);
            Assert.That(Number(ai, "attackHitTime"), Is.InRange(.25f, .35f), name);
            Assert.That(Number(ai, "attackAnimationSpeed"), Is.GreaterThan(1), name);
            Assert.That(Number(ai, "damage"), Is.LessThanOrEqualTo(20), name);
            if (name == "ArcherSkeleton")
                Assert.That(Number(ai, "projectileSpeed") * Number(ai, "projectileLifeTime"), Is.LessThanOrEqualTo(12));
        }
    }

    [TestCase("skeletonWarriorLoot")]
    [TestCase("ArcherSkeletonLoot")]
    [TestCase("DarkKnightLoot")]
    public void NormalLoot_HasBothEmptyAndSuccessfulRolls(string name)
    {
        var table = Asset("Assets/Data/LootTables/" + name + ".asset");
        Assert.That(new SerializedObject(table).FindProperty("guaranteeAtLeastOne").boolValue, Is.False);
        var random = new System.Random(20261106);
        var roll = table.GetType().GetMethod("Roll");
        int empty = 0;
        for (int i = 0; i < 2000; i++)
            if (!((IEnumerable)roll.Invoke(table, new object[] {random})).Cast<object>().Any()) empty++;
        Assert.That(empty, Is.InRange(200, 1600), name + " phải có cả lượt rơi đồ và lượt trắng.");
    }

    [Test]
    public void RecoveryAndScrolls_AreRare_AndBetterFromChestsAndBosses()
    {
        var normalNames = new[] {"skeletonWarriorLoot", "ArcherSkeletonLoot", "DarkKnightLoot"};
        float maxSmall = 0, maxBig = 0, maxScroll = 0;
        foreach (var name in normalNames)
        {
            var table = Asset("Assets/Data/LootTables/" + name + ".asset");
            maxSmall = Mathf.Max(maxSmall, DropChance(table, "SmallHealPot"));
            maxBig = Mathf.Max(maxBig, DropChance(table, "BigHealPot"));
            maxScroll = Mathf.Max(maxScroll, Number(table, "skillScrollChance"));
            Assert.That(DropChance(table, "magic_powder"), Is.LessThanOrEqualTo(.010001f));
        }
        Assert.That(maxSmall, Is.LessThanOrEqualTo(.015f));
        Assert.That(maxBig, Is.LessThanOrEqualTo(.003f));
        Assert.That(maxScroll, Is.LessThanOrEqualTo(.005001f));
        foreach (var name in new[] {"RewardChestLoot", "TenguLoot", "EvilWitchLoot"})
        {
            var table = Asset("Assets/Data/LootTables/" + name + ".asset");
            Assert.That(DropChance(table, "SmallHealPot"), Is.InRange(maxSmall + .01f, .2f));
            Assert.That(DropChance(table, "BigHealPot"), Is.InRange(maxBig + .01f, .1f));
            Assert.That(Number(table, "skillScrollChance"), Is.InRange(maxScroll + .01f, .15f));
            Assert.That(DropChance(table, "magic_powder"), Is.LessThanOrEqualTo(.120001f));
        }
    }

    private static float DropChance(ScriptableObject table, string itemName)
    {
        var item = Asset("Assets/Data/Items/" + itemName + ".asset");
        var entries = new SerializedObject(table).FindProperty("entries");
        for (int i = 0; i < entries.arraySize; i++)
        {
            var row = entries.GetArrayElementAtIndex(i);
            if (row.FindPropertyRelative("item").objectReferenceValue != item) continue;
            var rarity = new SerializedObject(item).FindProperty("rarity").enumValueIndex;
            return Mathf.Clamp01(new[] {.65f, .35f, .15f, .05f, .01f}[rarity] * row.FindPropertyRelative("chanceMultiplier").floatValue);
        }
        return 0;
    }

    [TestCase(16f / 9f, 12f, 6f, true)]
    [TestCase(4f / 3f, 9.8f, 7f, true)]
    [TestCase(21f / 9f, 17f, 7f, true)]
    [TestCase(16f / 9f, 17f, 0f, false)]
    public void CameraAggro_CoversVisibleCorners_ForDifferentAspectRatios(float aspect, float x, float y, bool expected)
    {
        var preview = EditorSceneManager.NewPreviewScene();
        try
        {
            var cameraObject = new GameObject("AwarenessTestCamera");
            SceneManager.MoveGameObjectToScene(cameraObject, preview);
            var camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = new Vector3(0, 0, -10);
            camera.orthographic = true;
            camera.orthographicSize = 7.8f;
            camera.aspect = aspect;
            var actor = new GameObject("AwarenessTestActor");
            SceneManager.MoveGameObjectToScene(actor, preview);
            actor.transform.position = new Vector3(x, y, 0);
            var method = Type.GetType("EnemyAwareness, Assembly-CSharp").GetMethod("IsInView");
            Assert.That((bool)method.Invoke(null, new object[] {camera, actor.transform, null}), Is.EqualTo(expected));
        }
        finally { EditorSceneManager.ClosePreviewScene(preview); }
    }

    [Test]
    public void BossSpells_UseCompatibleProjectiles_AndLeaveDodgeWindows()
    {
        var witch = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy/EvilWitch/EvilWitch.prefab");
        var fan = new SerializedObject(Component(witch, "EvilWitchSpreadFireball")).FindProperty("fireballPrefab").objectReferenceValue;
        Assert.That(fan, Is.Not.Null);
        Assert.That(fan.GetType().Name, Is.EqualTo("Projectile"));
        var radial = (GameObject)new SerializedObject(Component(witch, "SpreadFireball")).FindProperty("fireBallPrefab").objectReferenceValue;
        Assert.That(radial.GetComponents<MonoBehaviour>().Any(c => c != null && c.GetType().Name == "Projectile"), Is.True);
        var homing = Component(witch, "EvilWitchHomingFireball");
        Assert.That(Number(homing, "maxLifeTime"), Is.LessThan(Number(homing, "skillCooldown")));
        Assert.That(Number(homing, "projectileSpeed"), Is.LessThan(5));
        Assert.That(Number(homing, "turnSpeed"), Is.LessThanOrEqualTo(120));
        var tengu = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy/Tengu/Tengu.prefab");
        Assert.That(Number(Component(tengu, "TenguFireBall"), "warningTime"), Is.GreaterThanOrEqualTo(.9f));
    }

    [Test]
    public void PlayerBaseline_IsConsistent_AcrossSceneTransitions()
    {
        var original = SceneManager.GetActiveScene();
        var values = new HashSet<int>();
        foreach (var guid in AssetDatabase.FindAssets("t:Scene", new[] {"Assets/Scenes"}))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var scene = SceneManager.GetSceneByPath(path);
            bool opened = !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                foreach (var health in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<MonoBehaviour>(true))
                    .Where(c => c != null && c.GetType().Name == "Health"))
                {
                    if (Number(health, "team") != 0) continue;
                    values.Add((int)Number(health, "maxHealth"));
                    Assert.That(Number(health, "invincibleTime"), Is.InRange(.5f, 1f), path);
                }
            }
            finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
        }
        SceneManager.SetActiveScene(original);
        Assert.That(values.Count, Is.EqualTo(1), "Máu không được đổi bất ngờ chỉ vì player thuộc scene khác.");
    }
}

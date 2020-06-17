﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

public class SkillManager : MonoBehaviour
{
    public static List<BulletModifier> temporaryBulletMods = new List<BulletModifier>();
    public EquippedWeapon equippedWeapon;

    [SerializeField, Header("Important")]
    private bool forceSkillRewrite = false;

    #region Skill Register & Load
    private Dictionary<string, SkillBase> registeredSkills = new Dictionary<string, SkillBase>();

    [SerializeField, Tooltip("Skill database-like prefab")]
    private GameObject prefabSkillLoader = null;
    /// <summary>
    /// Get all skills in-game from database object
    /// </summary>
    public void FillRegisteredSkills()
    {
        if (prefabSkillLoader == null)
        {
            Debug.LogError("Skill loader prefab not assigned! Can't load skills because of that");
        }
        else
        {
            var skillContainer = prefabSkillLoader.GetComponent<SkillPullFromDatabase>();
            if (skillContainer != null)
            {
                foreach (var skill in skillContainer.LoadSkills().Values)
                {
                    registeredSkills.Add(skill.SkillName(), skill);
                }
            }
            else
            {
                Debug.LogError("Skill loader has no database-pull-script assigned! Can't load skills because of that");
            }   
        }
    }

    public bool SaveSkill(string name, SkillBase skill)
    {
        if (!registeredSkills.ContainsKey(name))
        {
            registeredSkills.Add(name, skill);
            return true;
        }
        else
        {
            return false;
        }
    }

    public void PrintRegisteredSkills()
    {
        print($"Skills registered: {registeredSkills.Count}");
        foreach (var skill in registeredSkills.Keys)
        {
            print(skill + " " + registeredSkills[skill]);
        }
    }

    public SkillBase LoadSkill(string name)
    {
        //print(name);
        return registeredSkills[name];
    }

    private string fileName = "progress.bin";

    private void SaveSkills()
    {
        BinaryFormatter binaryformatter = new BinaryFormatter();
        FileStream file = File.Create(Application.persistentDataPath + fileName);
        SkillsRecord skillsSavedInfo;
        if (equippedWeapon != null) {
            skillsSavedInfo = new SkillsRecord(skills, activeSkills, equippedWeapons, equippedWeapon.weaponIndex);
        }
        else {
            skillsSavedInfo = new SkillsRecord(skills, activeSkills, equippedWeapons, 0);
        }
        //foreach (var skill in skillsSavedInfo.nonEquiptedWeapons)
        binaryformatter.Serialize(file, skillsSavedInfo);

        file.Close();
    }

    /// <summary>
    /// Loads skills by name. Grab skill information from "registered" skills
    /// </summary>
    private void LoadSkills()
    {
        if (!forceSkillRewrite && File.Exists(Application.persistentDataPath + fileName))
        {
            BinaryFormatter binaryformatter = new BinaryFormatter();
            FileStream file = File.Open(Application.persistentDataPath + fileName, FileMode.Open);
            var skillsSavedInfo = (SkillsRecord)binaryformatter.Deserialize(file);
            file.Close();

            skills = new List<SkillBase>();
            if (skillsSavedInfo.equiptedActiveSkills != null)
            {
                activeSkills = new List<EquippedActiveSkill>();
                foreach (var skill in skillsSavedInfo.equiptedActiveSkills)
                {
                    if (!String.IsNullOrEmpty(skill))
                        AddSkill(Instantiate(registeredSkills[skill] as ActiveSkill));
                }
            }
            if (skillsSavedInfo.nonEquiptedWeapons != null)
            {
                equippedWeapons = new List<EquippedWeapon>();
                foreach (var skill in skillsSavedInfo.equiptedWeaponsSkills)
                {
                    if (!String.IsNullOrEmpty(skill))
                        AddSkill(Instantiate(registeredSkills[skill] as WeaponSkill));
                }
            }
            if (equippedWeapons.Count > 0)
            {
                equippedWeapon = equippedWeapons[skillsSavedInfo.currentWeaponIndex];
                attackManager.LoadNewWeapon(equippedWeapon);
                ApplyWeaponSprites();
            }

            foreach (var skill in skillsSavedInfo.nonEquiptedActiveSkills)
            {
                if (!String.IsNullOrEmpty(skill))
                {
                    var skilInst = Instantiate(registeredSkills[skill] as ActiveSkill);
                    skills.Add(skilInst);
                    inventoryActiveSkills.Add(skilInst);
                }
            }
            foreach (var skill in skillsSavedInfo.passiveSkills)
            {
                if (!String.IsNullOrEmpty(skill)) skills.Add(Instantiate(registeredSkills[skill] as PassiveSkill));
            }
            foreach (var skill in skillsSavedInfo.nonEquiptedWeapons)
            {
                if (!String.IsNullOrEmpty(skill))
                {
                    var skilInst = Instantiate(registeredSkills[skill] as WeaponSkill);
                    skills.Add(skilInst);
                    inventoryWeaponSkills.Add(skilInst);
                }
            }

        }
        else
        {
            SaveSkills();
            if (!File.Exists(Application.persistentDataPath + fileName))
            {
                Debug.LogError("Critical error: save file was not created");
            }
            else
            {
                // Warning: Possible infinite loop here!!!
                forceSkillRewrite = false;
                LoadSkills();
            }
        }
    }

    #endregion

    [Serializable]
    public class EquippedActiveSkill
    {
        public ActiveSkill skill;
        public float cooldown;
        public float activeTimeLeft;

        public EquippedActiveSkill(ActiveSkill skill)
        {
            this.skill = skill;
            cooldown = 0;
            activeTimeLeft = 0;
        }
    }

    [Serializable]
    public class EquippedWeapon
    {
        public WeaponSkill logic;
        public int ammoLeft;
        public float reloadTimeLeft;
        public float lastTimeEquipped;
        public int weaponIndex;
        public AudioClip[] attackSound;

        public EquippedWeapon(WeaponSkill weapon, int weaponIndex)
        {
            this.logic = weapon;
            ammoLeft = weapon.ammoMagazine;
            reloadTimeLeft = 0;
            this.weaponIndex = weaponIndex;
            attackSound = weapon.attackSound;
            lastTimeEquipped = Time.time;
        }
    }

    private void Awake()
    {
        RelodScene.OnSceneChange.AddListener(SaveSkills);
        skillsUI = GameObject.FindGameObjectWithTag("Canvas").GetComponent<SkillsUI>();
    }

    List<WeaponSkill> inventoryWeaponSkills = new List<WeaponSkill>();
    List<ActiveSkill> inventoryActiveSkills = new List<ActiveSkill>();

    public List<WeaponSkill> InventoryWeaponSkill 
    { 
      get { return inventoryWeaponSkills; } 
      set { inventoryWeaponSkills = value; }
    }
    
    public List<ActiveSkill> InventoryActiveSkills
    {
        get { return inventoryActiveSkills; }
        set { inventoryActiveSkills = value; }
    }

    public List<EquippedWeapon> EquippedWeapons
    {
        get { return equippedWeapons; }
        set { equippedWeapons = value; }
    }

    public List<EquippedActiveSkill> ActiveSkills
    {
        get { return activeSkills; }
        set { activeSkills = value; }
    }

    public void ClearWeapons()
    {
        equippedWeapons.ForEach(weapon => skills.Remove(weapon.logic));
        equippedWeapon = null;
        attackManager.currentWeapon = null;
        equippedWeapons.Clear();
        equippedWeapons = new List<EquippedWeapon>();
    }

    public void AddSkill(SkillBase skill)
    {
        skills.Add(skill);
        skill.InitializeSkill();
        if (skill is ActiveSkill)
        {
            if (activeSkills.Count >= maxEquippedActiveCount)
            {
                inventoryActiveSkills.Add(skill as ActiveSkill);
            }
            else EquipActiveSkill(skill as ActiveSkill);
        }
        else if (skill is WeaponSkill)
        {
            if (equippedWeapons.Count >= maxEquippedWeaponCount)
            {
                inventoryWeaponSkills.Add(skill as WeaponSkill);
            }
            else EquipWeapon(skill as WeaponSkill);

        }
        RefreshUI();
    }

    private void Start()
    {
        temporaryBulletMods = new List<BulletModifier>();
        FillRegisteredSkills();
        //PrintRegisteredSkills();

        attackManager = GetComponent<CharacterShooting>();
        if (forceSkillRewrite)
            InitAfterRewrite();
        else
            LoadSkills();
        InitializeSkills();

        if (attackManager && equippedWeapons.Count != 0)
        {
            attackManager.LoadNewWeapon(equippedWeapon, instant: true);
        }
    }

    private void InitializeSkills()
    {
        foreach (var s in skills)
        {
            s.InitializeSkill();
        }
        if (!equippedWeapon.logic)
            equippedWeapon = equippedWeapons.Count != 0 ? equippedWeapons[0] : null;

        RefreshUI();
    }

    private void InitAfterRewrite()
    {

        foreach (var s in skills)
        {
            if (forceSkillRewrite)
            {
                if (s is ActiveSkill)
                {
                    if (activeSkills.Count >= 5)
                    {
                        inventoryActiveSkills.Add(s as ActiveSkill);
                    }
                    else
                    {
                        activeSkills.Add(new EquippedActiveSkill(s as ActiveSkill));
                    }
                }
                else if (s is WeaponSkill)
                {
                    if (equippedWeapons.Count >= 3)
                    {
                        inventoryWeaponSkills.Add(s as WeaponSkill);
                    }
                    else
                    {
                        equippedWeapons.Add(new EquippedWeapon(s as WeaponSkill, equippedWeapons.Count));
                    }
                }
            }
        }
    }

    private List<KeyCode> keys = new List<KeyCode>() {
        KeyCode.Alpha1,
        KeyCode.Alpha2,
        KeyCode.Alpha3,
        KeyCode.Alpha4,
        KeyCode.Alpha5,
        KeyCode.Alpha6,
        KeyCode.Alpha7,
        KeyCode.Alpha8,
        KeyCode.Alpha9,
        KeyCode.Alpha0
    };

    private void Update()
    {
        // Check for a key pressed for active skill
        for (int i = 0; i < activeSkills.Count; i++)
        {
            if (Input.GetKeyDown(keys[i]) && activeSkills.Count >= i && activeSkills[i].cooldown <= 0f)
            {
                activeSkills[i].skill.ActivateSkill();
                activeSkills[i].activeTimeLeft = activeSkills[i].skill.activeDuration;
                activeSkills[i].cooldown = activeSkills[i].skill.cooldownDuration;
            }
        }

        // Update effect, cooldown and active time left for active skill
        float[] skillCooldownsProportion = new float[SkillsUI.skillCount];
        bool[] isActiveSkill = new bool[SkillsUI.skillCount];
        for (int i = 0; i < activeSkills.Count; i++)
        {
            activeSkills[i].cooldown = Mathf.Max(0, activeSkills[i].cooldown - Time.deltaTime);

            if (activeSkills[i].activeTimeLeft > 0)
            {
                activeSkills[i].skill.UpdateEffect();
                activeSkills[i].activeTimeLeft = Mathf.Max(0, activeSkills[i].activeTimeLeft - Time.deltaTime);
                if (activeSkills[i].activeTimeLeft <= 0)
                {
                    activeSkills[i].skill.EndOfSkill();
                }
            }
            skillCooldownsProportion[i] = activeSkills[i].cooldown / activeSkills[i].skill.cooldownDuration;

            isActiveSkill[i] = activeSkills[i].activeTimeLeft > 0;
        }
        skillsUI.UpdateSkillRecoverVisualCooldown(skillCooldownsProportion, isActiveSkill);

        // Switch weapon
        if ((Input.GetKeyDown(rotateWeaponLeft) || Input.GetKeyDown(rotateWeaponRight)) && equippedWeapons.Count != 0)
        {
            var newWeaponIndex = 0;
            if (Input.GetKeyDown(rotateWeaponLeft))
                newWeaponIndex = (equippedWeapon.weaponIndex + equippedWeapons.Count - 1) % equippedWeapons.Count;
            else if (Input.GetKeyDown(rotateWeaponRight))
                newWeaponIndex = (equippedWeapon.weaponIndex + 1) % equippedWeapons.Count;
            if (equippedWeapon.ammoLeft < equippedWeapon.logic.ammoMagazine)
            {
                ReloadWeaponIfNeeded();
            }
            equippedWeapon = equippedWeapons[newWeaponIndex];
            foreach (var weapon in equippedWeapons)
                attackManager.LoadNewWeapon(equippedWeapon);
            ApplyWeaponSprites();
        }

        // Update reload time of all weapons & call update
        float[] weaponCooldownsProportion = new float[SkillsUI.weaponsCount];
        int j = 0;
        foreach (var weapon in equippedWeapons)
        {
            if (weapon.reloadTimeLeft != 0)
            {
                weapon.reloadTimeLeft = Mathf.Max(0, weapon.reloadTimeLeft - Time.deltaTime);
                weapon.ammoLeft = Mathf.Max(weapon.ammoLeft, (int)Mathf.Floor(Mathf.Lerp(weapon.logic.ammoMagazine, 0, (weapon.reloadTimeLeft - 0.01f) / weapon.logic.reloadTime)));
            }
            weaponCooldownsProportion[j] = weapon.reloadTimeLeft / weapon.logic.reloadTime;

            weapon.logic.UpdateEffect();
            j++;
        }
        if (equippedWeapons.Count != 0)
        {
            skillsUI.UpdateWeaponReloadVisualCooldown(weaponCooldownsProportion, equippedWeapon.weaponIndex);
            equippedWeapon.logic?.UpdateEquippedEffect();
        }

        // Update effect of passive skills
        foreach (var s in skills)
        {
            if (s is PassiveSkill)
            {
                s.UpdateEffect();
            }
        }

        // Update temporary weapon mods
        for (int i = 0; i < temporaryBulletMods.Count; i++)
        {
            temporaryBulletMods[i].modifierTime -= Time.deltaTime;
            if (temporaryBulletMods[i].modifierTime <= 0)
            {
                temporaryBulletMods.RemoveAt(i);
                i--;
            }
        }
    }

    public void ReloadWeaponIfNeeded()
    {
        if (equippedWeapon.reloadTimeLeft == 0 && equippedWeapon.ammoLeft < equippedWeapon.logic.ammoMagazine)
        {
            equippedWeapon.reloadTimeLeft = equippedWeapon.logic.reloadTime *
                Mathf.Lerp(1, 0.4f, (float)equippedWeapon.ammoLeft / equippedWeapon.logic.ammoMagazine); // more bullets = faster reload
        }
    }

    public void EquipWeapon(WeaponSkill skill)
    {
        equippedWeapons.Add(new EquippedWeapon(skill, equippedWeapons.Count));
        if (equippedWeapons.Count == 1) // There was no other weapons before we added this
        {
            equippedWeapon = equippedWeapons[0];
            attackManager.LoadNewWeapon(equippedWeapon, instant: true);
        }
        skill.InitializeSkill();
    }

    public void EquipActiveSkill(ActiveSkill skill)
    {
        activeSkills.Add(new EquippedActiveSkill(skill as ActiveSkill));
        skill.InitializeSkill();
    }

    #region UI block
    public void RefreshUI()
    {
        ApplyWeaponSprites();
        ApplySkillSprites();
    }

    public void ApplyWeaponSprites()
    {
        var weaponIcons = new Sprite[SkillsUI.weaponsCount];
        var weaponMiniIcons = new Sprite[SkillsUI.weaponsCount];
        for (int i = 0; i < equippedWeapons.Count; i++)
        {
            weaponIcons[i] = equippedWeapons[i].logic.pickupSprite;
            weaponMiniIcons[i] = equippedWeapons[i].logic.miniIcon;
        }
        skillsUI.SetWeaponSprites(weaponIcons, weaponMiniIcons, equippedWeapons.Count != 0 ? equippedWeapon.weaponIndex : 0);
    }

    public void ApplySkillSprites()
    {
        var skillIcons = new Sprite[SkillsUI.skillCount];
        for (int i = 0; i < activeSkills.Count; i++)
        {
            if (activeSkills[i] != null)
            {
                skillIcons[i] = activeSkills[i].skill.pickupSprite;
            }
        }
        skillsUI.SetSkillSprites(skillIcons);
    }
    #endregion

    public int maxEquippedActiveCount = 5;
    public int maxEquippedWeaponCount = 3;

    public List<SkillBase> skills = new List<SkillBase>();

    private List<EquippedActiveSkill> activeSkills = new List<EquippedActiveSkill>();

    private List<EquippedWeapon> equippedWeapons = new List<EquippedWeapon>();
    private KeyCode rotateWeaponLeft = KeyCode.Q;
    private KeyCode rotateWeaponRight = KeyCode.E;
    private CharacterShooting attackManager;
    private SkillsUI skillsUI;
}

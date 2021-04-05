using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TBQuestGame.Models;
using System.Collections.ObjectModel;
using TBQuestGame.DataLayer;
using System.Windows;

namespace TBQuestGame.PresentationLayer
{
    public class GameSessionViewModel : ObservableObject
    {
        //random object
        private Random random = new Random();

        #region FIELDS

        private Player _player;
        private string _gameMessage;
        private Map _masterGameMap;
        private Location _currentLocation;
        private string _currentLocationInformation;
        private ObservableCollection<Location> _accessibleLocations;
        private string _currentLocationName;
        private GameItem _currentGameItem;
        private ButtonVisibility _buttonVisibility;
        private Npc _currentNpc;

        #endregion

        #region PROPERTIES

        public Player Player
        {
            get { return _player; }
            set { _player = value; }
        }
        public string GameMessage
        {
            set 
            { 
                _gameMessage = value;
                OnPropertyChanged(nameof(GameMessage));
            }
            get
            {
                if (_player.NewPlayer)
                {
                    _gameMessage = GameData.DEFAULT_GAME_MESSAGE + "\n\n\n" + CurrentLocation.LocationMessage.ToString();
                }
                else
                {
                    _gameMessage = CurrentLocation.LocationMessage.ToString();
                }
                return _gameMessage; 
            }
        }
        public Map MasterGameMap
        {
            get { return _masterGameMap; }
            set { _masterGameMap = value; }
        }
        public Location CurrentLocation
        {
            get { return _currentLocation; }
            set
            {
                _currentLocation = value;
                _currentLocationInformation = _currentLocation.Description;
                OnPropertyChanged(nameof(CurrentLocation));
                OnPropertyChanged(nameof(CurrentLocationInformation));
            }       
        }
        public string CurrentLocationInformation
        {
            get { return _currentLocationInformation; }
            set
            {
                _currentLocationInformation = value;
                OnPropertyChanged(nameof(CurrentLocationInformation));
            }
        }
        public ObservableCollection<Location> AccessibleLocations
        {
            get { return _accessibleLocations; }
            set
            {
                _accessibleLocations = value;
                OnPropertyChanged(nameof(AccessibleLocations));
            }
        }
        public string CurrentLocationName
        {
            get { return _currentLocationName; }
            set 
            {
                _currentLocationName = value;
                OnPropertyChanged(nameof(CurrentLocationName));
            }
        }
        public GameItem CurrentGameItem
        {
            get { return _currentGameItem; }
            set { _currentGameItem = value; }
        }
        public ButtonVisibility ButtonVisibility
        {
            get { return _buttonVisibility; }
            set { _buttonVisibility = value; }
        }
        public Npc CurrentNpc
        {
            get { return _currentNpc; }
            set { _currentNpc = value; }
        }

        #endregion

        #region CONSTRUCTORS

        public GameSessionViewModel() {} //Default Constructor
        public GameSessionViewModel(Player player, Map masterGameMap, ButtonVisibility buttonVisibility)
        {
            _player = player;
            _masterGameMap = masterGameMap;
            _buttonVisibility = buttonVisibility;
            
            InitializeView();
        }

        #endregion

        #region METHODS

        private void Battle()
        {
            if (_currentNpc is IBattle)
            {
                IBattle battleNpc = _currentNpc as IBattle;
                int playerHitPoints = 0;
                int battleNpcHitPoints = 0;
                string battleInformation = "";

                playerHitPoints = CalculatePlayerHitPoints();
                if (battleNpc.CurrentWeapon != null)
                {
                    battleNpcHitPoints = CalculateNpcHitPoints(battleNpc);

                }
                else
                {
                    battleInformation = "It appears you are attacking an unarmed assailant" + Environment.NewLine;                
                }
                battleInformation +=
                    $"Player: {Player.BattleMode}        Hit Points:  {playerHitPoints} " + Environment.NewLine +
                    $"NPC: {battleNpc.BattleMode}        Hit Points:  {battleNpcHitPoints} " + Environment.NewLine;

                if (playerHitPoints >= battleNpcHitPoints)
                {
                    battleInformation += $"You have slain {_currentNpc.Name}" + Environment.NewLine +
                        "Check the items tab, the slain NPC may have dropped something";
                    //defeated Npc drops items
                    if (_currentNpc.Inventory != null)
                    {
                        foreach (var gameItem in _currentNpc.Inventory)
                        {
                            _currentLocation.AddGameItemToLocation(gameItem);
                        }

                    }

                    _currentLocation.Npcs.Remove(_currentNpc);

                }
                else
                {
                    battleInformation += $"you have been slain by {_currentNpc.Name}";
                    Player.Lives--;
                }
                CurrentLocationInformation = battleInformation;
                if (Player.Lives <= 0) OnPlayerDies("You have been slain and have no lives remaining.");
            }
            else
            {
                CurrentLocationInformation = "The current NPC is not battle ready";
            }

        }
        public void OnPlayerAttack()
        {
            Player.BattleMode = BattleModeName.ATTACK;
            Battle();
        }
        public void OnPlayerDefend()
        {
            Player.BattleMode = BattleModeName.DEFEND;
            Battle();
        }
        public void OnPlayerRetreat()
        {
            Player.BattleMode = BattleModeName.RETREAT;
            Battle();
        }
        public void OnPlayerTalkTo()
        {
            if (CurrentNpc != null && CurrentNpc is ISpeak)
            {
                ISpeak speakingNpc = CurrentNpc as ISpeak;
                CurrentLocationInformation = speakingNpc.Speak();
            }
        }
        private int CalculatePlayerHitPoints()
        {
            int playerHitPoints = 0;
            switch (Player.BattleMode)
            {
                case BattleModeName.ATTACK:
                    playerHitPoints = Player.Attack();
                    break;
                case BattleModeName.DEFEND:
                    playerHitPoints = Player.Defend();
                    break;
                case BattleModeName.RETREAT:
                    playerHitPoints = Player.Retreat();
                    break;
            }
            return playerHitPoints;
        }
        private BattleModeName NpcBattleResponse()
        {
            BattleModeName npcBattleResponse = BattleModeName.RETREAT;

            switch (DieRoll(3))
            {
                case 1:
                    npcBattleResponse = BattleModeName.ATTACK;
                    break;
                case 2:
                    npcBattleResponse = BattleModeName.DEFEND;
                    break;
                case 3:
                    npcBattleResponse = BattleModeName.RETREAT;
                    break;
            }
            return npcBattleResponse;
        }
        private int CalculateNpcHitPoints(IBattle battleNpc)
        {
            int npcHitPoints = 0;
            switch (NpcBattleResponse())
            {
                case BattleModeName.ATTACK:
                    npcHitPoints = battleNpc.Attack();
                    break;
                case BattleModeName.DEFEND:
                    npcHitPoints = battleNpc.Defend();
                    break;
                case BattleModeName.RETREAT:
                    npcHitPoints = battleNpc.Retreat();
                    break;
            }
            return npcHitPoints;
        }

        /// <summary>
        /// game start up methods
        /// </summary>
        private void InitializeView()
        {
            
            _currentLocation = _masterGameMap.CurrentLocation;
            _accessibleLocations = new ObservableCollection<Location>();
            UpdateAccessibleLocations();
            UpdateVisibleButtons();
            _player.InventoryUpdate();
            _player.CalculateWealth();
            UpdateAccessibleGameItems();
        }
        /// <summary>
        /// fires when a player clicks on a map area button
        /// </summary>
        /// <param name="areaID"></param>
        public void PlayerAdvance(int areaID)
        {
            Player.NewPlayer = false;

            foreach (Location location in _masterGameMap.Locations)
            {
                if (areaID == location.Id)
                {
                    CurrentLocation = location;
                    GameMessage = CurrentLocation.LocationMessage;
                    Player.LocationsVisited.Add(location);
                }
            }

            ModifyPlayerHealth();
            ModifyPlayerLives();
            UpdateAccessibleLocations();
            UpdateVisibleButtons();
        }
        /// <summary>
        /// updates areas that are accessible from current area
        /// </summary>
        private void UpdateAccessibleLocations()
        {
            //clear accessible locations
            _accessibleLocations.Clear();

            //start with no accessible locations
            foreach (Location location in _masterGameMap.Locations)
            {
                location.IsAccessible = false;
            }


            //update available locations based on current location
            foreach (int locationId in CurrentLocation.CurrentAvailableLocations)
            {
                foreach (Location location in _masterGameMap.Locations)
                {
                    if (location.Id == locationId)
                    {
                        location.IsAccessible = true;
                        _accessibleLocations.Add(location);
                    }
                }
            }
        }
        /// <summary>
        /// updates items that area available in certain areas
        /// </summary>
        private void UpdateAccessibleGameItems()
        {
            foreach (Location location in _masterGameMap.Locations)
            {
                for (int i = 0; i < location.GameItems.Count; i++)
                {
                    GameItem gameItem = location.GameItems[i];
                    if (!gameItem.IsAvailable)
                    {
                        location.RemoveGameItemFromLocation(gameItem);
                    }
                }
                    
            }
        }
        /// <summary>
        /// only areas that area accessible will show their buttons
        /// </summary>
        private void UpdateVisibleButtons()
        {
            foreach (Location location in _masterGameMap.Locations)
            {
                ButtonVisibility.ButtonUpdate(location);
            }
        }
        /// <summary>
        /// areas that modify player lives use this method when movement occurs
        /// </summary>
        private void ModifyPlayerLives()
        {
            if (CurrentLocation.ModifyLives != 0)
            {
                _player.Lives += CurrentLocation.ModifyLives;
            }
        }
        /// <summary>
        /// areas that modify player health use this method when movement occurs
        /// </summary>
        private void ModifyPlayerHealth()
        {
            if (CurrentLocation.ModifyHealth != 0)
            {
                _player.Health += CurrentLocation.ModifyHealth;
            }
        }
        /// <summary>
        /// removes game item from location and adds to player inventory
        /// </summary>
        public void AddItemToInventory()
        {

            if (_currentGameItem != null && _currentLocation.GameItems.Contains(_currentGameItem))
            {
                GameItem selectedGameItem = _currentGameItem as GameItem;

                //removes from location and adds to player inventory
                _currentLocation.RemoveGameItemFromLocation(selectedGameItem);
                _player.AddGameItemToInventory(selectedGameItem);

                OnPlayerPickUp(selectedGameItem);
            }
        }
        /// <summary>
        /// removes inventory from player and adds to location
        /// </summary>
        public void RemoveItemFromInventory()
        {

            if (_currentGameItem != null)
            {
                GameItem selectedGameItem = _currentGameItem as GameItem;

                _currentLocation.AddGameItemToLocation(selectedGameItem);
                _player.RemoveGameItemFromInventory(selectedGameItem);

                OnPlayerPutDown(selectedGameItem);
            }
        }
        /// <summary>
        /// adds any gameitem value to player
        /// </summary>
        /// <param name="gameItem"></param>
        private void OnPlayerPickUp(GameItem gameItem)
        {
            _player.Wealth += gameItem.Value;
        }
        /// <summary>
        /// removes value of game item and if currently selected weapon removes attack points
        /// </summary>
        /// <param name="gameItem"></param>
        private void OnPlayerPutDown(GameItem gameItem)
        {
            if (gameItem is Weapon weapon)
            {
                if (Player.AttackPoints == weapon.AttackPoints)
                {
                    //return to default weapon
                    Player.CurrentWeapon = GameData.GameItemById(1000) as Weapon;
                    Player.AttackPoints = Player.CurrentWeapon.AttackPoints;
                }
            }
            _player.Wealth -= gameItem.Value;
        }

        /// <summary>
        /// switch statement to process different game items
        /// </summary>
        public void OnUseGameItem()
        {
            switch (_currentGameItem)
            {
                case Spell spell:
                    ProcessSpellUse(spell);
                    break;
                case Artifact artifact:
                    ProcessArtifacUse(artifact);
                    break;
                case Weapon weapon:
                    ProcessWeaponUse(weapon);
                    break;
                default:
                    break;
            }
        }
        /// <summary>
        /// using a weapon 'arms' the player with the weapon
        /// </summary>
        /// <param name="weapon"></param>
        private void ProcessWeaponUse(Weapon weapon)
        {
            Player.CurrentWeapon = weapon;
            Player.AttackPoints = weapon.AttackPoints;
        }
        /// <summary>
        /// based on artifact use action switch statement to process different use actions
        /// </summary>
        /// <param name="artifact"></param>
        private void ProcessArtifacUse(Artifact artifact)
        {
            switch (artifact.UseAction)
            {
                case Artifact.UseActionType.KILL_PLAYER:
                    _player.Lives--;
                    OnPlayerDies(artifact.UseMessage);
                    break;
                case Artifact.UseActionType.OPEN_LOCATION:
                    PlayerAdvance(100);
                    break;
                default:
                    break;
            }
        }
        /// <summary>
        /// based on spell use action switch statement to process different use actions
        /// </summary>
        /// <param name="spell"></param>
        private void ProcessSpellUse(Spell spell)
        {
            _player.Health += spell.HealthChange;
            _player.Lives += spell.LivesChange;
            _player.RemoveGameItemFromInventory(_currentGameItem);
        }
        /// <summary>
        /// process message box when player dies
        /// </summary>
        /// <param name="message"></param>
        private void OnPlayerDies(string message = "")
        {
            if (_player.Lives == 0)
            {
                string messagetext = message + $"\n\nYOU DIED! You have {_player.Lives} lives remaining. \n\n Play Again?";

                string titleText = "Death";
                MessageBoxButton button = MessageBoxButton.YesNo;
                MessageBoxResult result = MessageBox.Show(messagetext, titleText, button);
                switch (result)
                {
                    case MessageBoxResult.Yes:
                        Environment.Exit(0);
                        break;
                    case MessageBoxResult.No:
                        Environment.Exit(0);
                        break;
                }
            }
            else
            {
                string messageText = message + $"\n\nYOU DIED! You have {_player.Lives} lives remaining.  You will start in the Village!";
                string titleText = "Death";
                MessageBoxButton button = MessageBoxButton.OK;
                MessageBox.Show(messageText, titleText, button);
                PlayerAdvance(100);

            }



        }

        #endregion

        #region HELPER METHODS

        private int DieRoll(int sides)
        {
            return random.Next(1, sides + 1);
        }
        #endregion

    }
}

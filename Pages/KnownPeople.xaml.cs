using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.ComponentModel;
using FirstFloor.ModernUI.Presentation;
using FirstFloor.ModernUI.Windows.Controls;
using Newtonsoft.Json;

namespace GeekAuctionDatabaseEditor.Pages {
    
    [ValueConversion(typeof(bool), typeof(bool))]
    public class InverseBooleanConverter: IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (targetType != typeof(bool))
                throw new InvalidOperationException("The target must be a boolean");
            return !(bool)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        #endregion
    }

    [ValueConversion(typeof(string), typeof(Uri))]
    public class IDToUriConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (targetType != typeof(Uri))
                throw new InvalidOperationException("The target must be an Uri");

            return "https://vk.com/id" + value;
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        #endregion
    }

    [ValueConversion(typeof(string), typeof(Uri))]
    public class NicknameToUriConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (targetType != typeof(Uri))
                throw new InvalidOperationException("The target must be an Uri");

            return "https://vk.com/" + value;
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        #endregion
    }

    public class Config
    {
        public string DbRepoUrl { get; set; }
        public string DbFile { get; set; }
        public string GitPath { get; set; }
    }

    /// <summary>
    /// Interaction logic for KnownPeople.xaml
    /// </summary>
    public partial class KnownPeople : UserControl
    {

        private Config config;

        public KnownPeople()
        {
            InitializeComponent();
            this.DataContext = new KnownPeopleModel();
            Dispatcher.ShutdownStarted += Dispatcher_ShutdownStarted;
        }

        private void dataGrid1_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            ((KnownPeopleModel)DataContext).MergeRequired = false;
            ((KnownPeopleModel)DataContext).MergeTarget = null;
            ((KnownPeopleModel)DataContext).PersonExistsMessage = null;
        }

        private void RemoveNameButton_Click(object sender, RoutedEventArgs e)
        {
            ((KnownPeopleModel)DataContext).Selected.Names.Remove((string)((Button)sender).Tag);
        }
        private void RemoveIDButton_Click(object sender, RoutedEventArgs e)
        {
            ((KnownPeopleModel)DataContext).Selected.IDs.Remove((long)((Button)sender).Tag);
        }
        private void RemoveNicknameButton_Click(object sender, RoutedEventArgs e)
        {
            ((KnownPeopleModel)DataContext).Selected.Nicknames.Remove((string)((Button)sender).Tag);
        }
        private void PositiveUpButton_Click(object sender, RoutedEventArgs e)
        {
            ((KnownPeopleModel)DataContext).Selected.Pluses += 1;
        }
        private void PositiveDownButton_Click(object sender, RoutedEventArgs e)
        {
            if (((KnownPeopleModel)DataContext).Selected.Pluses == 0) return;
            ((KnownPeopleModel)DataContext).Selected.Pluses -= 1;
        }
        private void NegativeUpButton_Click(object sender, RoutedEventArgs e)
        {
            ((KnownPeopleModel)DataContext).Selected.Minuses += 1;
        }
        private void NegativeDownButton_Click(object sender, RoutedEventArgs e)
        {
            if (((KnownPeopleModel)DataContext).Selected.Minuses == 0) return;
            ((KnownPeopleModel)DataContext).Selected.Minuses -= 1;
        }
        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrWhiteSpace(((KnownPeopleModel)DataContext).NewCredentials)) return;

            string what = (string)((Button)sender).Tag;

            if (CheckAllPersons(((KnownPeopleModel)DataContext).NewCredentials, what))
            {
                return;
            }
            switch (what)
            {
                case "name":
                    ((KnownPeopleModel)DataContext).Selected.Names.Add(((KnownPeopleModel)DataContext).NewCredentials);
                    break;
                case "nickname":
                    ((KnownPeopleModel)DataContext).Selected.Nicknames.Add(((KnownPeopleModel)DataContext).NewCredentials);
                    break;
                case "id":
                    try
                    {
                        ((KnownPeopleModel)DataContext).Selected.IDs.Add(Convert.ToInt64(((KnownPeopleModel)DataContext).NewCredentials));
                    }
                    catch (Exception)
                    {
                        return;
                    }
                    break;
            }
            ((KnownPeopleModel)DataContext).NewCredentials = "";
        }

        private void NewCredentials_TextChanged(object sender, TextChangedEventArgs e)
        {
        }

        private bool CheckAllPersons(string input, string what)
        {
            var persons = ((KnownPeopleModel)DataContext).Persons;
            var selected = ((KnownPeopleModel)DataContext).Selected;
            if (CheckSelectedPerson(selected, input, what))
            {
                ((KnownPeopleModel)DataContext).MergeRequired = false;
                ((KnownPeopleModel)DataContext).MergeTarget = null;
                return true;
            }
            foreach (var p in persons)
            {
                if (CheckOnePerson(p, input, what))
                {
                    ((KnownPeopleModel)DataContext).MergeRequired = true;
                    ((KnownPeopleModel)DataContext).MergeTarget = p;
                    return true;
                }
            }
            ((KnownPeopleModel)DataContext).MergeRequired = false;
            ((KnownPeopleModel)DataContext).MergeTarget = null;
            ((KnownPeopleModel)DataContext).PersonExistsMessage = null;
            return false;
        }

        private bool CheckOnePerson(Person person, string input, string what)
        {
            if ("nickname".Equals(what) && person.Nicknames.Contains(input))
            {
                ((KnownPeopleModel)DataContext).PersonExistsMessage = "Человек с таким ником уже существует (#" + person.Identifier + ")";
                return true;
            }
            else
            {
                try
                {
                    if ("id".Equals(what) && person.IDs.Contains(Convert.ToInt64(input)))
                    {
                        ((KnownPeopleModel)DataContext).PersonExistsMessage = "Человек с таким ID уже существует (#" + person.Identifier + ")";
                        return true;
                    }
                }
                catch (Exception)
                {
                }
            }
            return false;
        }

        private bool CheckSelectedPerson(Person person, string input, string what)
        {
            if ("nickname".Equals(what) && person.Nicknames.Contains(input))
            {
                ((KnownPeopleModel)DataContext).PersonExistsMessage = "Такой ник уже добавлен";
                return true;
            }
            else if ("name".Equals(what) && person.Names.Contains(input))
            {
                ((KnownPeopleModel)DataContext).PersonExistsMessage = "Такое имя уже добавлено";
                return true;
            }
            else
            {
                try
                {
                    if ("id".Equals(what) && person.IDs.Contains(Convert.ToInt64(input)))
                    {
                        ((KnownPeopleModel)DataContext).PersonExistsMessage = "Такой ID уже добавлен";
                        return true;
                    }
                }
                catch (Exception)
                {
                }
            }
            return false;
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.ToString()));
            e.Handled = true;
        }

        private void RemovePersonButton_Click(object sender, RoutedEventArgs e)
        {
            Unregister(((KnownPeopleModel)DataContext).Selected);
            ((KnownPeopleModel)DataContext).Persons.Remove(((KnownPeopleModel)DataContext).Selected);
            if (((KnownPeopleModel)DataContext).Persons.Count != 0)
            {
                ((KnownPeopleModel)DataContext).Selected = ((KnownPeopleModel)DataContext).Persons[0];
            }
            else
            {
                ((KnownPeopleModel)DataContext).Selected = null;
            }
        }

        private void AddPersonButton_Click(object sender, RoutedEventArgs e)
        {
            var persons = ((KnownPeopleModel)DataContext).Persons;
            Person person = new Person();
            int max = 0;
            foreach (var p in persons)
            {
                if (max < p.Identifier)
                {
                    max = p.Identifier;
                }
            }
            person.Identifier = max + 1;
            ((KnownPeopleModel)DataContext).Persons.Add(person);
            ((KnownPeopleModel)DataContext).Selected = person;
            Register(person);
        }

        private void MergeButton_Click(object sender, RoutedEventArgs e)
        {
            var selected = ((KnownPeopleModel)DataContext).Selected;
            var toMerge = ((KnownPeopleModel)DataContext).MergeTarget;
            foreach (var i in toMerge.Nicknames)
            {
                selected.Nicknames.Add(i);
            }
            foreach (var i in toMerge.IDs)
            {
                selected.IDs.Add(i);
            }
            foreach (var i in toMerge.Names)
            {
                selected.Names.Add(i);
            }
            selected.Pluses += toMerge.Pluses;
            selected.Minuses += toMerge.Minuses;
            if (String.IsNullOrWhiteSpace(selected.Comment))
            {
                selected.Comment = toMerge.Comment;
            }
            else
            {
                selected.Comment += "\r\n" + toMerge.Comment;
            }
            if (String.IsNullOrWhiteSpace(selected.ApproveComment))
            {
                selected.ApproveComment = toMerge.ApproveComment;
            }
            else
            {
                selected.ApproveComment += "\r\n" + toMerge.ApproveComment;
            }
            if (String.IsNullOrWhiteSpace(selected.BanComment))
            {
                selected.BanComment = toMerge.BanComment;
            }
            else
            {
                selected.BanComment += "\r\n" + toMerge.BanComment;
            }
            if (String.IsNullOrWhiteSpace(selected.BlacklistComment))
            {
                selected.BlacklistComment = toMerge.BlacklistComment;
            }
            else
            {
                selected.BlacklistComment += "\r\n" + toMerge.BlacklistComment;
            }
            selected.Approved |= toMerge.Approved;
            selected.Banned |= toMerge.Banned;
            selected.Blacklist |= toMerge.Blacklist;

            var persons = ((KnownPeopleModel)DataContext).Persons;

            Unregister(toMerge);
            persons.Remove(toMerge);

            ((KnownPeopleModel)DataContext).PersonExistsMessage = null;
            ((KnownPeopleModel)DataContext).MergeTarget = null;
            ((KnownPeopleModel)DataContext).MergeRequired = false;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var json = JsonConvert.SerializeObject(((KnownPeopleModel)DataContext).Persons, Formatting.Indented);
            File.WriteAllText("repo/" + config.DbFile, json, Encoding.UTF8);
            ((KnownPeopleModel)DataContext).HasChanges = false;
        }

        private void FilterPersonButton_Click(object sender, RoutedEventArgs e)
        {
            ((KnownPeopleModel)DataContext).FilteredPersons.Refresh();
        }

        private void Form_Loaded(object sender, RoutedEventArgs e)
        {
            if (File.Exists("config.json"))
            {
                var json = File.ReadAllText("config.json", Encoding.UTF8);
                config = JsonConvert.DeserializeObject<Config>(json);
                try
                {
                    var git = config.GitPath + "\\bin\\git.exe";
                    if (!Directory.Exists("repo"))
                    {
                        Process.Start(git, "-C . clone -q \"" + config.DbRepoUrl + "\" repo").WaitForExit();
                    }
                } catch (Exception ex)
                {
                    ModernDialog.ShowMessage(ex.ToString(), "Error", MessageBoxButton.OK);
                }
            }
            if (File.Exists("repo/" + config.DbFile))
            {
                var json = File.ReadAllText("repo/" + config.DbFile, Encoding.UTF8);
                var collection = JsonConvert.DeserializeObject<ObservableCollection<Person>>(json);
                foreach (var p in collection)
                {
                    ((KnownPeopleModel)DataContext).Persons.Add(p);
                    Register(p);
                }
                ((KnownPeopleModel)DataContext).Persons.CollectionChanged += CollectionAnyChangesListener;
            }
        }

        private void Register(Person p)
        {
            p.PropertyChanged += AnyPropertyChangeListener;
            p.Nicknames.CollectionChanged += CollectionAnyChangesListener;
            p.Names.CollectionChanged += CollectionAnyChangesListener;
            p.IDs.CollectionChanged += CollectionAnyChangesListener;
        }

        private void Unregister(Person p)
        {
            p.PropertyChanged -= AnyPropertyChangeListener;
            p.Nicknames.CollectionChanged -= CollectionAnyChangesListener;
            p.Names.CollectionChanged -= CollectionAnyChangesListener;
            p.IDs.CollectionChanged -= CollectionAnyChangesListener;
        }

        void CollectionAnyChangesListener(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            ((KnownPeopleModel)DataContext).HasChanges = true;
        }

        private void AnyPropertyChangeListener(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            ((KnownPeopleModel)DataContext).HasChanges = true;
        }

        private void Dispatcher_ShutdownStarted(object sender, EventArgs e)
        {
            if (((KnownPeopleModel)DataContext).HasChanges)
            {
                if (MessageBoxResult.Yes == MessageBox.Show("Сохранить несохраненные изменения?", "Сохранить?", MessageBoxButton.YesNo))
                {
                    SaveButton_Click(sender, null);
                }
            }
        }

        private void PushButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var git = config.GitPath + "\\bin\\git.exe";
                if (Directory.Exists("repo"))
                {
                    Process.Start(git, "-C repo add .").WaitForExit();
                    Process.Start(git, "-C repo commit -m updated").WaitForExit();
                    Process.Start(git, "-C repo push").WaitForExit();
                }
            }
            catch (Exception ex)
            {
                ModernDialog.ShowMessage(ex.ToString(), "Error", MessageBoxButton.OK);
            }
        }
    }

    public class Person : NotifyPropertyChanged
    {

        private ObservableCollection<string> names     = new ObservableCollection<string>();
        private ObservableCollection<long>   ids       = new ObservableCollection<long>();
        private ObservableCollection<string> nicknames = new ObservableCollection<string>();

        private string comment;

        private bool   approved;
        private string approveComment;
        private bool   banned;
        private string banComment;
        private bool   blacklist;
        private string blacklistComment;
        private int    pluses;
        private int    minuses;

        public Person()
        {
            OnPropertyChanged("");
        }
        public int Identifier { get; set; }
        public ObservableCollection<string> Names
        {
            get { return names; }
            set { names = value; OnPropertyChanged("Names"); }
        }
        public ObservableCollection<long> IDs
        {
            get { return ids; }
            set { ids = value; OnPropertyChanged("IDs"); }
        }
        public ObservableCollection<string> Nicknames
        {
            get { return nicknames; }
            set { nicknames = value; OnPropertyChanged("Nicknames"); }
        }
        public string Comment
        {
            get { return comment; }
            set { comment = value; OnPropertyChanged("Comment"); }
        }
        public string ApproveComment
        {
            get { return approveComment; }
            set { approveComment = value; OnPropertyChanged("ApproveComment"); }
        }
        public string BanComment
        {
            get { return banComment; }
            set { banComment = value; OnPropertyChanged("BanComment"); }
        }
        public string BlacklistComment
        {
            get { return blacklistComment; }
            set { blacklistComment = value; OnPropertyChanged("BlacklistComment"); }
        }
        public bool Approved
        {
            get { return approved; }
            set { approved = value; OnPropertyChanged("Approved"); }
        }
        public bool Banned
        {
            get { return banned; }
            set { banned = value; OnPropertyChanged("Banned"); }
        }
        public bool Blacklist
        {
            get { return blacklist; }
            set { blacklist = value; OnPropertyChanged("Blacklist"); }
        }
        public int Pluses
        {
            get { return pluses; }
            set { pluses = value; OnPropertyChanged("Pluses"); OnPropertyChanged("Reputation"); }
        }
        public int Minuses
        {
            get { return minuses; }
            set { minuses = value; OnPropertyChanged("Minuses"); OnPropertyChanged("Reputation"); }
        }
        [JsonIgnoreAttribute]
        public int Reputation { get { return Pluses - Minuses; } }

    }

    public class KnownPeopleModel : NotifyPropertyChanged
    {

        private Person selected;
        private string newCredentials;
        private string personExistsMessage;
        private bool   mergeRequired;
        private Person mergeTarget;
        private bool   hasChanges;
        private string filterText;
        private bool   filterByComment;

        public KnownPeopleModel()
        {
            Persons = new ObservableCollection<Person>();
            var itemSourceList = new CollectionViewSource() { Source = Persons };
            FilteredPersons = itemSourceList.View;
            FilteredPersons.Filter = new Predicate<object>((p) =>
            {
                if (String.IsNullOrWhiteSpace(this.filterText)) return true;
                var person = ((Person)p);
                foreach (var i in person.Nicknames)
                {
                    if (i.ToLower().Contains(this.filterText.ToLower()))
                    {
                        return true;
                    }
                }
                foreach (var i in person.IDs)
                {
                    if (("" + i).Contains(this.filterText.ToLower()))
                    {
                        return true;
                    }
                }
                foreach (var i in person.Names)
                {
                    if (i.ToLower().Contains(this.filterText.ToLower()))
                    {
                        return true;
                    }
                }

                if (this.filterByComment)
                {
                    if (!String.IsNullOrWhiteSpace(person.Comment) &&
                        person.Comment.ToLower().Contains(this.filterText.ToLower()))
                    {
                        return true;
                    }
                    if (!String.IsNullOrWhiteSpace(person.ApproveComment) &&
                        person.ApproveComment.ToLower().Contains(this.filterText.ToLower()))
                    {
                        return true;
                    }
                    if (!String.IsNullOrWhiteSpace(person.BanComment) &&
                        person.BanComment.ToLower().Contains(this.filterText.ToLower()))
                    {
                        return true;
                    }
                    if (!String.IsNullOrWhiteSpace(person.BlacklistComment) &&
                        person.BlacklistComment.ToLower().Contains(this.filterText.ToLower()))
                    {
                        return true;
                    }
                }

                return false;
            });
        }

        public ObservableCollection<Person> Persons { get; set; }
        public Person Selected
        {
            get { return selected; }
            set { selected = value; OnPropertyChanged("Selected"); OnPropertyChanged("IsSelected"); OnPropertyChanged("IsNotSelected"); }
        }
        public bool IsSelected
        {
            get { return selected != null; }
        }
        public bool IsNotSelected
        {
            get { return selected == null; }
        }
        public string NewCredentials
        {
            get { return newCredentials; }
            set { newCredentials = value; OnPropertyChanged("NewCredentials"); }
        }
        public bool MergeRequired
        {
            get { return mergeRequired; }
            set { mergeRequired = value; OnPropertyChanged("MergeRequired"); }
        }
        public Person MergeTarget
        {
            get { return mergeTarget; }
            set { mergeTarget = value; OnPropertyChanged("MergeTarget"); }
        }
        public string PersonExistsMessage
        {
            get { return personExistsMessage; }
            set { personExistsMessage = value; OnPropertyChanged("PersonExistsMessage"); OnPropertyChanged("HasPersonExistsMessage"); }
        }
        public bool HasPersonExistsMessage
        {
            get { return personExistsMessage != null; }
        }
        public bool HasChanges
        {
            get { return hasChanges; }
            set { hasChanges = value; OnPropertyChanged("HasChanges"); }
        }
        public string FilterText
        {
            get { return filterText; }
            set { filterText = value; OnPropertyChanged("FilterText"); }
        }
        public bool FilterByComment
        {
            get { return filterByComment; }
            set { filterByComment = value; OnPropertyChanged("FilterByComment"); }
        }
        public ICollectionView FilteredPersons
        {
            get;
            set;
        }
    }
}

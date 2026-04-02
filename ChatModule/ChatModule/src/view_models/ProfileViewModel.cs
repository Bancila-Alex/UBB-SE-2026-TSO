using ChatModule.Models;
using ChatModule.Services;
using ChatModule.src.domain.Enums;
using ChatModule.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace ChatModule.ViewModels;
public class ProfileViewModel : BaseViewModel
{
    private readonly FriendRequestService _friendRequestService;
    private readonly BlockService _blockService;
    private readonly DirectMessageService _directMessageService;
    private readonly Guid _currentUserId;
    private readonly ProfileService _profileService;

    private User? _user;
    public User? User
    {
        get => _user;
        set => Set(ref _user, value);
    }

    private bool _isBlocked;
    public bool IsBlocked
    {
        get => _isBlocked;
        set => Set(ref _isBlocked, value);
    }

    private bool _isOwnProfile;
    public bool IsOwnProfile
    {
        get => _isOwnProfile;
        set => Set(ref _isOwnProfile, value);
    }

    public ObservableCollection<User> MutualFriends { get; } = new();

    public event Action<Guid>? NavigateToChatRequested;

    public RelayCommand SendFriendRequestCommand { get; }
    public RelayCommand BlockUserCommand { get; }
    public RelayCommand UnblockUserCommand { get; }
    public RelayCommand OpenDmCommand { get; }

    private UserStatus _selectedStatus;
    public UserStatus SelectedStatus
    {
        get => _selectedStatus;
        set
        {
            if (Set(ref _selectedStatus, value) && User != null)
            {
                _ = UpdateStatusAsync(value);
            }
        }
    }

    private string? _editBio;
    public string? EditBio
    {
        get => _editBio;
        set => Set(ref _editBio, value);
    }

    private string? _editAvatarUrl;
    public string? EditAvatarUrl
    {
        get => _editAvatarUrl;
        set => Set(ref _editAvatarUrl, value);
    }

    private DateTime? _editBirthday;
    public DateTime? EditBirthday
    {
        get => _editBirthday;
        set
        {
            if (Set(ref _editBirthday, value))
            {
                OnPropertyChanged(nameof(EditBirthdayOffset));
                OnPropertyChanged(nameof(IsBirthdayToday));
            }
        }
    }

    public RelayCommand SaveProfileCommand { get; }
    public RelayCommand LoadMutualFriendsCommand { get; }
    public ObservableCollection<UserStatus> AvailableStatuses { get; } =
    [
        UserStatus.Online,
        UserStatus.Offline,
        UserStatus.Busy
    ];

    public DateTimeOffset EditBirthdayOffset
    {
        get => EditBirthday.HasValue
            ? new DateTimeOffset(EditBirthday.Value.Date)
            : DateTimeOffset.Now;
        set
        {
            EditBirthday = value.DateTime.Date;
            OnPropertyChanged(nameof(IsBirthdayToday));
        }
    }

    public bool IsBirthdayToday
    {
        get
        {
            if (!EditBirthday.HasValue)
            {
                return false;
            }

            var today = DateTime.Today;
            return EditBirthday.Value.Month == today.Month && EditBirthday.Value.Day == today.Day;
        }
    }

    public ProfileViewModel(
        FriendRequestService friendRequestService,
        BlockService blockService,
        DirectMessageService directMessageService,
        ProfileService profileService,
        Guid currentUserId)
    {
        _friendRequestService = friendRequestService ?? throw new ArgumentNullException(nameof(friendRequestService));
        _blockService = blockService ?? throw new ArgumentNullException(nameof(blockService));
        _directMessageService = directMessageService ?? throw new ArgumentNullException(nameof(directMessageService));
        _currentUserId = currentUserId;
        _profileService = profileService ?? throw new ArgumentNullException(nameof(ProfileService));
        
        SendFriendRequestCommand = new RelayCommand(SendFriendRequestAsync);
        BlockUserCommand = new RelayCommand(BlockUserAsync);
        UnblockUserCommand = new RelayCommand(UnblockUserAsync);
        OpenDmCommand = new RelayCommand(OpenDmAsync);

        SaveProfileCommand = new RelayCommand(SaveProfileAsync);
        LoadMutualFriendsCommand = new RelayCommand(LoadMutualFriendsAsync);
    }

    public async Task LoadAsync(Guid targetUserId)
    {
        User = await _profileService.GetProfileAsync(targetUserId);
        IsOwnProfile = targetUserId == _currentUserId;

        if (User != null)
        {
            SelectedStatus = User.Status;
            EditBio = User.Bio;
            EditAvatarUrl = User.AvatarUrl;
            EditBirthday = User.Birthday;
        }

        await LoadMutualFriendsAsync();
    }

    private async Task UpdateStatusAsync(UserStatus status)
    {
        if (User == null) return;
        await _profileService.UpdateStatusAsync(User.Id, status);
    }

    private async Task SaveProfileAsync()
    {
        if (User == null) return;
        await _profileService.UpdateProfileAsync(User.Id, EditBio, EditAvatarUrl, EditBirthday);
        User = await _profileService.GetProfileAsync(User.Id);
    }

    private async Task LoadMutualFriendsAsync()
    {
        MutualFriends.Clear();
        if (!IsOwnProfile && User != null)
        {
            var mutuals = await _profileService.GetMutualFriendsAsync(_currentUserId, User.Id);
            foreach (var user in mutuals)
                MutualFriends.Add(user);
        }
    }

    private async Task SendFriendRequestAsync()
    {
        if (User == null)
        {
            return;
        }

        await _friendRequestService.SendRequestAsync(_currentUserId, User.Id);
    }

    private async Task BlockUserAsync()
    {
        if (User == null)
        {
            return;
        }

        await _blockService.BlockUserAsync(_currentUserId, User.Id);
        IsBlocked = true;
    }

    private async Task UnblockUserAsync()
    {
        if (User == null)
        {
            return;
        }

        await _blockService.UnblockUserAsync(_currentUserId, User.Id);
        IsBlocked = false;
    }

    private async Task OpenDmAsync()
    {
        if (User == null)
        {
            return;
        }

        var conversation = await _directMessageService.GetOrCreateAsync(_currentUserId, User.Id);
        NavigateToChatRequested?.Invoke(conversation.Id);
    }
}

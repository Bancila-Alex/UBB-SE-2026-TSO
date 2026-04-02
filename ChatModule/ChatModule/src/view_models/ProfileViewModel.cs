using System;
using System.Threading.Tasks;
using ChatModule.Models;
using ChatModule.Services;
using ChatModule.ViewModels;
using System.Collections.ObjectModel;

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
    }

    public async Task LoadAsync(Guid targetUserId)
    {
        User = await _profileService.GetProfileAsync(targetUserId);
        IsOwnProfile = targetUserId == _currentUserId;

        MutualFriends.Clear();
        if (!IsOwnProfile)
        {
            var mutuals = await _profileService.GetMutualFriendsAsync(_currentUserId, targetUserId);
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

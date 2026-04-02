using System;
using System.Threading.Tasks;
using ChatModule.Models;
using ChatModule.Services;
using ChatModule.ViewModels;

namespace ChatModule.src.view_models
{
    public class ProfileViewModel : BaseViewModel
    {
        private readonly FriendRequestService _friendRequestService;
        private readonly BlockService _blockService;
        private readonly DirectMessageService _directMessageService;
        private readonly Guid _currentUserId;

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

        public event Action<Guid>? NavigateToChatRequested;

        public RelayCommand SendFriendRequestCommand { get; }
        public RelayCommand BlockUserCommand { get; }
        public RelayCommand UnblockUserCommand { get; }
        public RelayCommand OpenDmCommand { get; }

        public ProfileViewModel(
            FriendRequestService friendRequestService,
            BlockService blockService,
            DirectMessageService directMessageService,
            Guid currentUserId)
        {
            _friendRequestService = friendRequestService ?? throw new ArgumentNullException(nameof(friendRequestService));
            _blockService = blockService ?? throw new ArgumentNullException(nameof(blockService));
            _directMessageService = directMessageService ?? throw new ArgumentNullException(nameof(directMessageService));
            _currentUserId = currentUserId;

            SendFriendRequestCommand = new RelayCommand(SendFriendRequestAsync);
            BlockUserCommand = new RelayCommand(BlockUserAsync);
            UnblockUserCommand = new RelayCommand(UnblockUserAsync);
            OpenDmCommand = new RelayCommand(OpenDmAsync);
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
}

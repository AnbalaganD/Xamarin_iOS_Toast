using System;
using UIKit;
using CoreGraphics;
using System.Threading.Tasks;
using CoreFoundation;
using System.Threading;

namespace Toast
{
    public enum ToastDuration
    {
        Short, Long
    }

    public class Toast
    {
        const float MINIMUN_TOAST_HEIGHT = 35;
        const float MINIMUN_TOAST_WIDTH = 100;
        const float LEFT_RIGHT_PADDING = 20;
        const float MESSAGE_LABEL_LEFT_RIGHT_PADDING = 20;
        const float MESSAGE_LABEL_TOP_BOTTOM_PADDING = 10;
        const float TOAST_BOTTOM_PADDING = 40;

        static Toast shared;
        public static Toast Shared => shared ?? (shared = new Toast());

        bool isVisible;

        UILabel toastLabel;
        UIView toastContainer;
        CancellationTokenSource source;

        Toast() { }

        public void Show(string message, ToastDuration duration = ToastDuration.Short)
        {
            if (isVisible)
            {
                if (toastContainer == null)
                    return;

                toastLabel.Text = message;
                toastContainer.Layer.CornerRadius = GetCornerRadius(toastLabel.IntrinsicContentSize);
                toastContainer.LayoutIfNeeded();
                toastContainer.UpdateConstraintsIfNeeded();
                var temp = ShowToast(duration);
                return;
            }

            isVisible = true;
            var superView = GetTopViewController(UIApplication.SharedApplication.KeyWindow.RootViewController)?.View;

            if (superView == null)
                return;

            toastLabel = new UILabel
            {
                TranslatesAutoresizingMaskIntoConstraints = false,
                LineBreakMode = UILineBreakMode.WordWrap,
                TextAlignment = UITextAlignment.Center,
                TextColor = UIColor.White,
                Lines = 0,
                Text = message,
                Font = UIFont.SystemFontOfSize(16)
            };

            toastContainer = new UIView
            {
                TranslatesAutoresizingMaskIntoConstraints = false,
                BackgroundColor = UIColor.Black.ColorWithAlpha(0.7f),
                ClipsToBounds = true,
                Alpha = 0
            };

            toastContainer.Add(toastLabel);
            superView.Add(toastContainer);
            ApplyConstraint(superView);
            var ignore = ShowToast(duration);
            toastContainer.Layer.CornerRadius = GetCornerRadius(toastLabel.IntrinsicContentSize);
        }

        nfloat GetCornerRadius(CGSize size)
        {
            var height = size.Height;
            if ((UIScreen.MainScreen.Bounds.Width - (2 * LEFT_RIGHT_PADDING)) < size.Width)
            {
                var numberOfIteration = (int)Math.Round(size.Width / UIScreen.MainScreen.Bounds.Width);
                for (int i = 0; i < numberOfIteration; i++)
                {
                    height += size.Height;
                }
            }
            return (height + 20) / 2;
        }

        void ApplyConstraint(UIView superView)
        {
            if (UIDevice.CurrentDevice.CheckSystemVersion(9, 0))
            {
                toastLabel.LeadingAnchor.ConstraintEqualTo(toastContainer.LeadingAnchor, MESSAGE_LABEL_LEFT_RIGHT_PADDING).Active = true;
                toastLabel.TrailingAnchor.ConstraintEqualTo(toastContainer.TrailingAnchor, -MESSAGE_LABEL_LEFT_RIGHT_PADDING).Active = true;
                toastLabel.TopAnchor.ConstraintEqualTo(toastContainer.TopAnchor, MESSAGE_LABEL_TOP_BOTTOM_PADDING).Active = true;
                toastLabel.BottomAnchor.ConstraintEqualTo(toastContainer.BottomAnchor, -MESSAGE_LABEL_TOP_BOTTOM_PADDING).Active = true;

                toastContainer.CenterXAnchor.ConstraintEqualTo(superView.CenterXAnchor).Active = true;
                var toastWidthAnchor = toastContainer.WidthAnchor.ConstraintGreaterThanOrEqualTo(MINIMUN_TOAST_WIDTH);
                toastWidthAnchor.Priority = 999;
                toastWidthAnchor.Active = true;
                toastContainer.LeadingAnchor.ConstraintGreaterThanOrEqualTo(superView.LeadingAnchor, LEFT_RIGHT_PADDING).Active = true;
                toastContainer.TrailingAnchor.ConstraintLessThanOrEqualTo(superView.TrailingAnchor, -LEFT_RIGHT_PADDING).Active = true;

                if (UIDevice.CurrentDevice.CheckSystemVersion(11, 0))
                    toastContainer.BottomAnchor.ConstraintEqualTo(superView.SafeAreaLayoutGuide.BottomAnchor, -TOAST_BOTTOM_PADDING).Active = true;
                else
                    toastContainer.BottomAnchor.ConstraintEqualTo(superView.BottomAnchor, -TOAST_BOTTOM_PADDING).Active = true;
            }
            else
            {
                NSLayoutConstraint.Create(toastLabel, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, toastContainer, NSLayoutAttribute.Leading, 1, MESSAGE_LABEL_LEFT_RIGHT_PADDING).Active = true;
                NSLayoutConstraint.Create(toastLabel, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, toastContainer, NSLayoutAttribute.Trailing, 1, -MESSAGE_LABEL_LEFT_RIGHT_PADDING).Active = true;
                NSLayoutConstraint.Create(toastLabel, NSLayoutAttribute.Top, NSLayoutRelation.Equal, toastContainer, NSLayoutAttribute.Top, 1, MESSAGE_LABEL_TOP_BOTTOM_PADDING).Active = true;
                NSLayoutConstraint.Create(toastLabel, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, toastContainer, NSLayoutAttribute.Bottom, 1, -MESSAGE_LABEL_TOP_BOTTOM_PADDING).Active = true;

                NSLayoutConstraint.Create(toastContainer, NSLayoutAttribute.CenterX, NSLayoutRelation.GreaterThanOrEqual, superView, NSLayoutAttribute.CenterX, 1, 0).Active = true;
                var toastWidthAnchor = NSLayoutConstraint.Create(toastContainer, NSLayoutAttribute.Width, NSLayoutRelation.GreaterThanOrEqual, 1, MINIMUN_TOAST_WIDTH);
                toastWidthAnchor.Priority = 999;
                toastWidthAnchor.Active = true;

                NSLayoutConstraint.Create(toastContainer, NSLayoutAttribute.Leading, NSLayoutRelation.GreaterThanOrEqual, superView, NSLayoutAttribute.Leading, 1, LEFT_RIGHT_PADDING).Active = true;
                NSLayoutConstraint.Create(toastContainer, NSLayoutAttribute.Trailing, NSLayoutRelation.GreaterThanOrEqual, superView, NSLayoutAttribute.Trailing, 1, -LEFT_RIGHT_PADDING).Active = true;
                NSLayoutConstraint.Create(toastContainer, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, superView, NSLayoutAttribute.Bottom, 1, -TOAST_BOTTOM_PADDING).Active = true;
            }
        }

        async Task ShowToast(ToastDuration duration)
        {
            GetTopViewController(UIApplication.SharedApplication.KeyWindow.RootViewController)?.View.EndEditing(true);
            if (toastContainer != null)
            {
                await UIView.AnimateAsync(0.5, () =>
                {
                    toastContainer.Alpha = 1.0f;
                });
            }
            await StartTimer(duration);
        }

        async Task HideToast()
        {
            await UIView.AnimateAsync(0.5, () =>
            {
                toastContainer.Alpha = 0.0f;
            });
            toastContainer.RemoveFromSuperview();
            isVisible = false;
        }

        async Task StartTimer(ToastDuration duration)
        {
            source?.Cancel();
            source = new CancellationTokenSource();

            try
            {
                await Task.Delay(duration == ToastDuration.Short ? 4000 : 6000).ContinueWith((arg) =>
                {
                    if (arg.IsCompleted)
                    {
                        DispatchQueue.MainQueue.DispatchAsync(async () => { await HideToast(); });
                    }
                }, source.Token);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        UIViewController GetTopViewController(UIViewController controller)
        {
            if (controller == null)
                return null;

            if (controller is UINavigationController)
            {
                var navigationController = controller as UINavigationController;
                return GetTopViewController(navigationController.VisibleViewController);
            }
            if (controller is UITabBarController)
            {
                var tabBarControll = controller as UITabBarController;
                return GetTopViewController(tabBarControll.SelectedViewController);
            }
            if (controller.PresentedViewController != null)
                return GetTopViewController(controller.PresentedViewController);

            return controller;
        }
    }
}
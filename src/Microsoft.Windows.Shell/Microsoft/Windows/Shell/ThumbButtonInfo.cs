using Standard;

using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Microsoft.Windows.Shell
{
    [DefaultEvent("Click")]
    public sealed class ThumbButtonInfo : Freezable, ICommandSource
    {
        private EventHandler _commandEvent;
        public static readonly DependencyProperty VisibilityProperty = DependencyProperty.Register(nameof(Visibility), typeof(Visibility), typeof(ThumbButtonInfo), new PropertyMetadata((object)Visibility.Visible));
        public static readonly DependencyProperty DismissWhenClickedProperty = DependencyProperty.Register(nameof(DismissWhenClicked), typeof(bool), typeof(ThumbButtonInfo), new PropertyMetadata((object)false));
        public static readonly DependencyProperty ImageSourceProperty = DependencyProperty.Register(nameof(ImageSource), typeof(ImageSource), typeof(ThumbButtonInfo), new PropertyMetadata((PropertyChangedCallback)null));
        public static readonly DependencyProperty IsBackgroundVisibleProperty = DependencyProperty.Register(nameof(IsBackgroundVisible), typeof(bool), typeof(ThumbButtonInfo), new PropertyMetadata((object)true));
        public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register(nameof(Description), typeof(string), typeof(ThumbButtonInfo), new PropertyMetadata((object)string.Empty, (PropertyChangedCallback)null, new CoerceValueCallback(ThumbButtonInfo._CoerceDescription)));
        public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.Register(nameof(IsEnabled), typeof(bool), typeof(ThumbButtonInfo), new PropertyMetadata((object)true, (PropertyChangedCallback)null, (CoerceValueCallback)((d, e) => ((ThumbButtonInfo)d)._CoerceIsEnabledValue(e))));
        public static readonly DependencyProperty IsInteractiveProperty = DependencyProperty.Register(nameof(IsInteractive), typeof(bool), typeof(ThumbButtonInfo), new PropertyMetadata((object)true));
        public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(ThumbButtonInfo), new PropertyMetadata((object)null, (PropertyChangedCallback)((d, e) => ((ThumbButtonInfo)d)._OnCommandChanged(e))));
        public static readonly DependencyProperty CommandParameterProperty = DependencyProperty.Register(nameof(CommandParameter), typeof(object), typeof(ThumbButtonInfo), new PropertyMetadata((object)null, (PropertyChangedCallback)((d, e) => ((ThumbButtonInfo)d)._UpdateCanExecute())));
        public static readonly DependencyProperty CommandTargetProperty = DependencyProperty.Register(nameof(CommandTarget), typeof(IInputElement), typeof(ThumbButtonInfo), new PropertyMetadata((object)null, (PropertyChangedCallback)((d, e) => ((ThumbButtonInfo)d)._UpdateCanExecute())));
        private static readonly DependencyProperty _CanExecuteProperty = DependencyProperty.Register(nameof(_CanExecute), typeof(bool), typeof(ThumbButtonInfo), new PropertyMetadata((object)true, (PropertyChangedCallback)((d, e) => d.CoerceValue(ThumbButtonInfo.IsEnabledProperty))));

        protected override Freezable CreateInstanceCore() => (Freezable)new ThumbButtonInfo();

        public Visibility Visibility
        {
            get => (Visibility)this.GetValue(ThumbButtonInfo.VisibilityProperty);
            set => this.SetValue(ThumbButtonInfo.VisibilityProperty, (object)value);
        }

        public bool DismissWhenClicked
        {
            get => (bool)this.GetValue(ThumbButtonInfo.DismissWhenClickedProperty);
            set => this.SetValue(ThumbButtonInfo.DismissWhenClickedProperty, (object)value);
        }

        public ImageSource ImageSource
        {
            get => (ImageSource)this.GetValue(ThumbButtonInfo.ImageSourceProperty);
            set => this.SetValue(ThumbButtonInfo.ImageSourceProperty, (object)value);
        }

        public bool IsBackgroundVisible
        {
            get => (bool)this.GetValue(ThumbButtonInfo.IsBackgroundVisibleProperty);
            set => this.SetValue(ThumbButtonInfo.IsBackgroundVisibleProperty, (object)value);
        }

        public string Description
        {
            get => (string)this.GetValue(ThumbButtonInfo.DescriptionProperty);
            set => this.SetValue(ThumbButtonInfo.DescriptionProperty, (object)value);
        }

        private static object _CoerceDescription(DependencyObject d, object value)
        {
            string str = (string)value;
            if (str != null && str.Length >= 260)
                str = str.Substring(0, 259);
            return (object)str;
        }

        private object _CoerceIsEnabledValue(object value) => (!(bool)value ? 0 : (this._CanExecute ? 1 : 0)) != 0;

        public bool IsEnabled
        {
            get => (bool)this.GetValue(ThumbButtonInfo.IsEnabledProperty);
            set => this.SetValue(ThumbButtonInfo.IsEnabledProperty, (object)value);
        }

        public bool IsInteractive
        {
            get => (bool)this.GetValue(ThumbButtonInfo.IsInteractiveProperty);
            set => this.SetValue(ThumbButtonInfo.IsInteractiveProperty, (object)value);
        }

        private void _OnCommandChanged(DependencyPropertyChangedEventArgs e)
        {
            ICommand oldValue = (ICommand)e.OldValue;
            ICommand newValue = (ICommand)e.NewValue;
            if (oldValue == newValue)
                return;
            if (oldValue != null)
                this._UnhookCommand(oldValue);
            if (newValue == null)
                return;
            this._HookCommand(newValue);
        }

        private bool _CanExecute
        {
            get => (bool)this.GetValue(ThumbButtonInfo._CanExecuteProperty);
            set => this.SetValue(ThumbButtonInfo._CanExecuteProperty, (object)value);
        }

        public event EventHandler Click;

        internal void InvokeClick()
        {
            EventHandler click = this.Click;
            if (click != null)
                click((object)this, EventArgs.Empty);
            this._InvokeCommand();
        }

        private void _InvokeCommand()
        {
            ICommand command = this.Command;
            if (command == null)
                return;
            object commandParameter = this.CommandParameter;
            IInputElement commandTarget = this.CommandTarget;
            if (command is RoutedCommand routedCommand)
            {
                if (routedCommand.CanExecute(commandParameter, commandTarget))
                    routedCommand.Execute(commandParameter, commandTarget);
            }
            else if (command.CanExecute(commandParameter))
                command.Execute(commandParameter);
        }

        private void _UnhookCommand(ICommand command)
        {
            Assert.IsNotNull<ICommand>(command);
            command.CanExecuteChanged -= this._commandEvent;
            this._commandEvent = (EventHandler)null;
            this._UpdateCanExecute();
        }

        private void _HookCommand(ICommand command)
        {
            this._commandEvent = (EventHandler)((sender, e) => this._UpdateCanExecute());
            command.CanExecuteChanged += this._commandEvent;
            this._UpdateCanExecute();
        }

        private void _UpdateCanExecute()
        {
            if (this.Command != null)
            {
                object commandParameter = this.CommandParameter;
                IInputElement commandTarget = this.CommandTarget;
                if (this.Command is RoutedCommand command2)
                    this._CanExecute = command2.CanExecute(commandParameter, commandTarget);
                else
                    this._CanExecute = this.Command.CanExecute(commandParameter);
            }
            else
                this._CanExecute = true;
        }

        public ICommand Command
        {
            get => (ICommand)this.GetValue(ThumbButtonInfo.CommandProperty);
            set => this.SetValue(ThumbButtonInfo.CommandProperty, (object)value);
        }

        public object CommandParameter
        {
            get => this.GetValue(ThumbButtonInfo.CommandParameterProperty);
            set => this.SetValue(ThumbButtonInfo.CommandParameterProperty, value);
        }

        public IInputElement CommandTarget
        {
            get => (IInputElement)this.GetValue(ThumbButtonInfo.CommandTargetProperty);
            set => this.SetValue(ThumbButtonInfo.CommandTargetProperty, (object)value);
        }
    }
}

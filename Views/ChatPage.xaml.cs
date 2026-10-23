using Microsoft.Maui.Controls.Shapes;

namespace TutorLocal;

public partial class ChatPage : ContentPage
{
    public ChatPage()
    {
        InitializeComponent();
    }

    private async void OnBackButtonClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private async void OnSendMessageClicked(object sender, EventArgs e)
    {
        // 1. Récupérer le texte tapé
        string messageText = MessageEntry.Text;

        if (!string.IsNullOrWhiteSpace(messageText))
        {
            // 2. Créer une nouvelle bulle utilisateur en C#
            var newBubble = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) },
                ColumnSpacing = 8,
                Margin = new Thickness(0, 0, 0, 20) // Espace en bas
            };

            var stackLayout = new VerticalStackLayout
            {
                Spacing = 4,
                HorizontalOptions = LayoutOptions.End
            };

            // Construction de la bordure bleue
            var border = new Border
            {
                BackgroundColor = Color.FromArgb("#5B44E9"),
                StrokeThickness = 0,
                Padding = new Thickness(16, 12),
                MaximumWidthRequest = 280,
                StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(16, 16, 4, 16) }
            };

            var label = new Label
            {
                Text = messageText,
                FontSize = 14,
                TextColor = Colors.White,
                LineHeight = 1.4
            };

            border.Content = label;

            // Construction de l'heure et des checks
            var timeLayout = new HorizontalStackLayout
            {
                HorizontalOptions = LayoutOptions.End,
                Spacing = 4,
                Margin = new Thickness(0, 0, 4, 0)
            };
            
            timeLayout.Children.Add(new Label { Text = DateTime.Now.ToString("HH:mm"), FontSize = 10, TextColor = Color.FromArgb("#6B7280") });
            timeLayout.Children.Add(new Label { Text = "✓", FontSize = 10, TextColor = Color.FromArgb("#5B44E9"), FontAttributes = FontAttributes.Bold });

            // Assemblage
            stackLayout.Children.Add(border);
            stackLayout.Children.Add(timeLayout);
            newBubble.Children.Add(stackLayout);
            Grid.SetColumn(stackLayout, 0);

            // 3. Ajouter la bulle à la liste existante (définie dans le XAML)
            MessageList.Children.Add(newBubble);

            // 4. Vider le champ de saisie
            MessageEntry.Text = string.Empty;

            // 5. Faire défiler automatiquement vers le bas pour voir le nouveau message
            await Task.Delay(100); // Petit délai pour laisser le rendu se faire
            await ChatScrollView.ScrollToAsync(newBubble, ScrollToPosition.End, true);
        }
    }

    private async void OnNavigateToChat(object sender, EventArgs e)
{
    await Navigation.PushAsync(new ChatPage());
}
}
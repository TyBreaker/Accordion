Public Class Form1

    Const MAXCARDS As Byte = 52
    Const MAXSUITS As Byte = 4
    Const MAXRANKS As Byte = 13
    Const NOCHOICE As Byte = 100 ' bogus position indicating that no card has been selected yet

    Structure PlayingCard
        Dim rank As Integer
        Dim suit As Integer
        Dim revealed As Boolean ' face-up?
        Dim face As Image
        Dim rear As Image
    End Structure

    Dim cardButtons(MAXCARDS - 1) As Button ' each pile is a button
    Dim shuffled(MAXCARDS - 1), unshuffled(MAXCARDS - 1) As PlayingCard ' card deck
    Dim tabled As Byte = 0 ' number of piles currently face up
    Dim cardCount As Byte = 0 ' number of cards played - including those no longer visible
    Dim suits() As String = {"clubs", "diamonds", "hearts", "spades"}
    Dim ranks() As String = {"2", "3", "4", "5", "6", "7", "8", "9", "10", "jack", "queen", "king", "ace"}
    Dim cardWidth As Single = My.Resources.Red_Back.Width ' get width of card graphics
    Dim cardHeight As Single = My.Resources.Red_Back.Height ' get height of card graphics
    Dim displayCardWidth As Single = 0 ' the on-screen width of each card sized to fit the window
    Dim displayCardHeight As Single = 0 ' the on-screen height of each card sized to fit the window
    Dim cardMargin As Single = 2 ' space between piles
    Dim choices() As Integer = {NOCHOICE, NOCHOICE} ' pair of piles selected

    Private Function InsideOutShuffle(deck() As PlayingCard) As PlayingCard()
        ' Fisher-Yates shuffle algorithm
        Dim i, j As Integer
        Dim newShuffled(MAXCARDS - 1) As PlayingCard
        newShuffled(0) = deck(0)
        For i = 1 To MAXCARDS - 1
            j = CInt(Math.Floor((i + 1) * Rnd()))
            newShuffled(i) = newShuffled(j)
            newShuffled(j) = deck(i)
        Next i
        Return newShuffled
    End Function

    Private Sub InitDeck()
        ' open a new deck
        Dim k As Integer = 0
        For i As Integer = 0 To MAXSUITS - 1
            For j As Integer = 0 To MAXRANKS - 1
                unshuffled(k).suit = i ' assign its suit
                unshuffled(k).rank = j ' assign its rank
                unshuffled(k).revealed = False ' face down
                unshuffled(k).face = My.Resources.ResourceManager.GetObject("_" + ranks(j) + "_of_" + suits(i))
                unshuffled(k).rear = My.Resources.Red_Back
                k += 1
            Next
        Next
    End Sub

    Private Sub MovePiles()
        My.Computer.Audio.Play(My.Resources.card_drop, AudioPlayMode.Background)
        ' we found a match so we're moving piles
        Dim swapTemp As Byte
        If choices(0) > choices(1) Then ' ensure we order the cards correctly
            swapTemp = choices(1)
            choices(1) = choices(0)
            choices(0) = swapTemp
        End If
        ' remove the highlight
        cardButtons(choices(0)).FlatAppearance.BorderColor = Color.OliveDrab
        cardButtons(choices(1)).FlatAppearance.BorderColor = Color.OliveDrab
        ' replace the first pile
        shuffled(choices(0)).face = shuffled(choices(1)).face
        shuffled(choices(0)).rank = shuffled(choices(1)).rank
        shuffled(choices(0)).suit = shuffled(choices(1)).suit
        cardButtons(choices(0)).BackgroundImage = cardButtons(choices(1)).BackgroundImage
        For i As Byte = choices(1) To MAXCARDS - 2 ' transfer tailing piles up one position to fill the gap
            shuffled(i).face = shuffled(i + 1).face
            shuffled(i).rank = shuffled(i + 1).rank
            shuffled(i).suit = shuffled(i + 1).suit
            shuffled(i).revealed = shuffled(i + 1).revealed
            cardButtons(i).BackgroundImage = cardButtons(i + 1).BackgroundImage
        Next
        ' shrink the list of piles on screen by one
        If cardCount < MAXCARDS Then
            cardButtons(tabled).Visible = False ' erase the last face-up pile - face-down pile follows
        ElseIf cardCount = MAXCARDS Then
            cardButtons(tabled - 1).Visible = False ' erase the last face-up pile - no face-down pile follows
        End If
        choices = {NOCHOICE, NOCHOICE} ' no piles should now be selected
        tabled -= 1 ' we have one less pile
    End Sub

    Private Sub AssessAttempt()
        ' check if the selected piles are a match
        If (shuffled(choices(0)).rank = shuffled(choices(1)).rank Or shuffled(choices(0)).suit = shuffled(choices(1)).suit) And _
            (Math.Abs(choices(0) - choices(1)) = 1 Or Math.Abs(choices(0) - choices(1)) = 3) Then ' we have a match
            MovePiles()
        Else ' no match
            cardButtons(choices(0)).FlatAppearance.BorderColor = Color.OliveDrab
            cardButtons(choices(1)).FlatAppearance.BorderColor = Color.OliveDrab
            choices = {NOCHOICE, NOCHOICE}
        End If
    End Sub

    Private Function MatchesRemain() As Boolean
        ' check for suit and rank matches in tabled cards
        Dim potentialMatch As Byte
        MatchesRemain = False
        For i As Byte = 0 To tabled - 1
            potentialMatch = i + 1 ' check for adjacent matches
            If potentialMatch < tabled Then
                If shuffled(i).rank = shuffled(potentialMatch).rank Or shuffled(i).suit = shuffled(potentialMatch).suit Then MatchesRemain = True
            End If
            potentialMatch = i + 3
            If potentialMatch < tabled Then ' check for matches separated by two piles
                If shuffled(i).rank = shuffled(potentialMatch).rank Or shuffled(i).suit = shuffled(potentialMatch).suit Then MatchesRemain = True
            End If
        Next
    End Function

    Private Sub cardClick(sender As Object, e As EventArgs)
        ' determine if we're revealing a face-down card or selecting a face-up pile
        If shuffled(DirectCast(sender, Button).Name).revealed Then ' we're selecting a face-up pile
            If choices(0) = NOCHOICE Then ' first of pair selected
                DirectCast(sender, Button).FlatAppearance.BorderColor = Color.Gold
                choices(0) = DirectCast(sender, Button).Name
            ElseIf choices(1) = NOCHOICE Then ' second of pair selected, do we have a match?
                DirectCast(sender, Button).FlatAppearance.BorderColor = Color.Gold
                choices(1) = DirectCast(sender, Button).Name
                AssessAttempt() ' check for match
                If Not MatchesRemain() And cardCount = MAXCARDS Then
                    If tabled = 1 Then ' player won
                        My.Computer.Audio.Play(My.Resources.hooray, AudioPlayMode.Background)
                        MsgBox("You won!", MsgBoxStyle.Exclamation, "Game Over")
                        For Each cardButton In cardButtons ' prevent further user play
                            cardButton.Enabled = False
                        Next
                    Else ' player lost
                        My.Computer.Audio.Play(My.Resources.defeat, AudioPlayMode.Background)
                        MsgBox("No matches remain", MsgBoxStyle.Exclamation, "Game Over")
                        For Each cardButton In cardButtons ' prevent further user play
                            cardButton.Enabled = False
                        Next
                    End If
                End If
            End If
            If choices(0) = choices(1) Then ' clicking on same card twice deselects
                DirectCast(sender, Button).FlatAppearance.BorderColor = Color.OliveDrab
                DirectCast(sender, Button).FlatAppearance.BorderColor = Color.OliveDrab
                choices = {NOCHOICE, NOCHOICE}
            End If
        Else ' we're revealing a face-down card
            My.Computer.Audio.Play(My.Resources.card_drop, AudioPlayMode.Background)
            shuffled(DirectCast(sender, Button).Name).revealed = True
            cardButtons(DirectCast(sender, Button).Name).BackgroundImage = My.Resources.ResourceManager.GetObject("_" + ranks(shuffled(DirectCast(sender, Button).Name).rank) + "_of_" + suits(shuffled(DirectCast(sender, Button).Name).suit)) ' shows its face
            tabled += 1 ' we now have an extra card on the table
            cardCount += 1 ' we have made our way through the deck by one more card
            If cardCount < MAXCARDS Then
                cardButtons(DirectCast(sender, Button).Name + 1).Visible = True ' if face-down cards remain, display the face-down pile at the end
            Else ' no more cards, is game over?
                If Not MatchesRemain Then
                    My.Computer.Audio.Play(My.Resources.defeat, AudioPlayMode.Background)
                    MsgBox("No matches remain", MsgBoxStyle.Exclamation, "Game Over")
                    For Each cardButton In cardButtons ' prevent further user play
                        cardButton.Enabled = False
                    Next
                End If
            End If
        End If
    End Sub

    Private Sub InitTableau()
        ' create a button for each pile and place on table
        Dim xoffset As Single = cardMargin ' horizontal space between piles
        Dim yoffset As Single = cardMargin ' vertical space between piles
        Dim i As Byte = 0
        For y As Byte = 0 To 3
            For x As Byte = 0 To 12
                If cardButtons(i) IsNot Nothing Then ' erase previous game data
                    cardButtons(i).Visible = False
                    RemoveHandler cardButtons(i).Click, AddressOf cardClick
                    Me.Panel1.Controls.Remove(cardButtons(i))
                    cardButtons(i).Dispose()
                End If
                ' create this game's pile buttons
                cardButtons(i) = New Button
                cardButtons(i).Enabled = True
                cardButtons(i).Name = i
                cardButtons(i).FlatStyle = FlatStyle.Flat
                cardButtons(i).FlatAppearance.BorderSize = 3
                cardButtons(i).FlatAppearance.BorderColor = Color.OliveDrab
                cardButtons(i).BackgroundImage = My.Resources.Red_Back ' initially face down artwork
                cardButtons(i).BackgroundImageLayout = ImageLayout.Stretch
                cardButtons(i).Size = New Size(displayCardWidth, displayCardHeight)
                cardButtons(i).Location = New Point(x * (displayCardWidth + cardMargin) + xoffset, y * (displayCardHeight + cardMargin) + yoffset)
                If i > 0 Then cardButtons(i).Visible = False ' initially, only the first pile is visible
                Me.Panel1.Controls.Add(cardButtons(i))
                AddHandler cardButtons(i).Click, AddressOf cardClick
                i += 1
            Next
        Next
    End Sub

    Private Sub NewGame()
        My.Computer.Audio.Play(My.Resources.shuffling_cards, AudioPlayMode.Background)
        System.Threading.Thread.Sleep(500)
        shuffled = InsideOutShuffle(unshuffled) ' shuffle the deck
        tabled = 0 ' no face-up cards currently on table
        cardCount = 0 ' no cards yet played
        choices = {NOCHOICE, NOCHOICE} ' no pile selections made
        InitTableau() ' display piles
    End Sub

    Private Sub UpdateCards()
        ' each time the window is resized, recalculate the new pile button sizes
        Dim xoffset As Single = cardMargin ' horizontal space between piles
        Dim yoffset As Single = cardMargin ' vertical space between piles
        Dim i As Byte = 0
        For y As Byte = 0 To 3
            For x As Byte = 0 To 12
                If cardButtons(i) IsNot Nothing Then ' ignore this event when it fires during initial startup
                    cardButtons(i).Size = New Size(displayCardWidth, displayCardHeight)
                    cardButtons(i).Location = New Point(x * (displayCardWidth + cardMargin) + xoffset, y * (displayCardHeight + cardMargin) + yoffset)
                End If
                i += 1
            Next
        Next
    End Sub

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Randomize()
        InitDeck()
    End Sub

    Private Sub Form1_SizeChanged(sender As Object, e As EventArgs) Handles Me.SizeChanged
        ' deal with a change in window size
        Const maxCardsHeight As Integer = 4 ' number of vertical piles
        Const maxCardsWidth As Integer = 13 ' number of horizontal piles
        ' using window height to guide card size - first calculate according to window height
        displayCardHeight = (Me.Panel1.Height - maxCardsHeight * cardMargin) / maxCardsHeight
        displayCardWidth = displayCardHeight * (cardWidth / cardHeight)
        If displayCardWidth > (Me.Panel1.Width - maxCardsWidth * cardMargin) / maxCardsWidth Then ' if too wide, recalculate according to window width
            displayCardWidth = (Me.Panel1.Width - maxCardsWidth * cardMargin) / maxCardsWidth
            displayCardHeight = displayCardWidth * (cardHeight / cardWidth)
        End If
        UpdateCards() ' change all pile button sizes
    End Sub

    Private Sub PlayToolStripMenuItem_Click_1(sender As Object, e As EventArgs) Handles PlayToolStripMenuItem.Click
        NewGame()
    End Sub

    Private Sub HowToPlayToolStripMenuItem1_Click(sender As Object, e As EventArgs) Handles HowToPlayToolStripMenuItem1.Click
        MsgBox("Accordion is a solitaire game using one deck of playing cards." + vbCrLf + _
                "The object is to compress the entire deck into one pile like an" + vbCrLf + _
                "accordion.  A pile can be moved on top of another pile" + vbCrLf + _
                "immediately to its left or separated to its left by two " + vbCrLf + _
                "piles if the top cards of each pile have the same suit" + vbCrLf + _
                "or rank. Gaps left behind are filled by moving remaining" + vbCrLf + _
                "piles to the left." + vbCrLf + vbCrLf + _
                "Win by having only one pile left at the end.", MsgBoxStyle.Information, "How To Play")
    End Sub

    Private Sub AboutToolStripMenuItem1_Click(sender As Object, e As EventArgs) Handles AboutToolStripMenuItem1.Click
        MsgBox("Freeware by Tyson Ackland, December 2013", MsgBoxStyle.Information, "About Accordion Solitaire")
    End Sub

End Class

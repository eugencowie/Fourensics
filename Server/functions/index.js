const functions = require('firebase-functions');
const admin = require('firebase-admin');

admin.initializeApp();

exports.playerJoinedLobby = functions.database.ref('/lobbies/{id}/users/{uid}/user-id').onCreate((snapshot, context) => {

    const getLobbyUserIds = "0123".split('')
        .filter(x => x !== context.params.uid)
        .map(x => admin.database().ref(`/lobbies/${context.params.id}/users/${x}/user-id`).once('value'));

    return Promise.all(getLobbyUserIds).then(results => {

        const getUserNotificationTokens = results
            .map(x => admin.database().ref(`/users/${x.val()}/notification-token`).once('value'));

        return Promise.all(getUserNotificationTokens);

    }).then(results => {

        const notificationTokens = results
            .map(x => x.val())
            .filter(x => !!x);

        const payload = {
            notification: {
                title: 'Player has joined the game!',
                body: 'A new player has joined the lobby!'
            }
        };

        return admin.messaging().sendToDevice(notificationTokens, payload);

    });

});

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

exports.playerLeftLobby = functions.database.ref('/lobbies/{id}/users/{uid}/user-id').onDelete((snapshot, context) => {

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
                title: 'Player has left the game!',
                body: 'A player has left the lobby!'
            }
        };

        return admin.messaging().sendToDevice(notificationTokens, payload);

    });

});

exports.lobbyStarted = functions.database.ref('/lobbies/{id}/state').onUpdate((snapshot, context) => {

    const getLobbyUserIds = "0123".split('')
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
                title: 'The game has started!',
                body: 'The lobby has been started!'
            }
        };

        return admin.messaging().sendToDevice(notificationTokens, payload);

    });

});

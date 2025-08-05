# Demande de vérification - API Tags TextLab

## Contexte
Le client TextLab rencontre un problème avec l'endpoint d'association de tags aux documents.

## Problème observé
- **Endpoint** : `POST /api/v1/documents/{documentId}/tags`
- **Erreur** : `UnprocessableEntity - {"detail":[{"type":"value_error","loc":["body",0,"tag_id"],"msg":"Value error, Fournir soit tag_id soit tag_name","input":"e3c9a8fc-659e-4950-bf65-8880b0d68cf8","ctx":{"error":{}}}]}`
- **Attendu** : Association réussie

## Détails techniques
- **Authentication** : ✅ Correcte (X-User-Token header)
- **Payload JSON** : ❌ Format incorrect - client envoie objet DocumentTag complet
- **Tag existant** : ✅ Vérifié avant association
- **HTTP Status** : ❌ 422 UnprocessableEntity

## Questions pour l'équipe serveur
1. **Quel est le format exact attendu pour l'association** : seulement `{"tag_id": "xxx"}` ou `{"tag_name": "xxx"}` ?
2. **Faut-il envoyer les métadonnées** (Weight, Confidence, Source) ou seulement l'identifiant du tag ?
3. **Le format de réponse attendu** : `List<DocumentTag>` ou structure différente ?

## Informations additionnelles
- Les tags sont créés correctement avec `POST /api/v1/tags`
- La récupération fonctionne avec `GET /api/v1/documents/{documentId}/tags`
- Seule l'association pose problème

**Merci de vérifier la cohérence de l'endpoint d'association et son format de réponse.**
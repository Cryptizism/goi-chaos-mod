using BepInEx;
using BepInEx.Configuration;
using System.Collections;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using DentedPixel;
using UnityEngine.UIElements;

namespace ChaosMod
{
    [BepInPlugin("me.cryptizism.plugins.goi-chaos-mod", "Chaos Mod", "1.0.0.0")]
    [BepInProcess("GettingOverIt.exe")]
    public class Plugin : BaseUnityPlugin
    {
        GameObject player;
        GameObject mountain; //InterAction<bool> thingy
        GameObject hammer;
        GameObject woodenBarrel;
        GameObject orange;
        GameObject cam;
        GameObject narrator;

        Dictionary<String, Action<bool>> effects = new Dictionary<string, Action<bool>>();
        List<Action<bool>> runningEffects = new List<Action<bool>>();
        List<Action<bool>> doNotRepeat = new List<Action<bool>>();

        bool isMain;

        Vector3 spawnLoc = new Vector3(-44.28097F, -2.422038F, 0);

        //setup
        AssetBundle assetBundle;
        int effectLast = 25;
        int effectEvery = 20;
        int offset = 4;

        GameObject votingHolder;
        TextMeshProUGUI votestxt;

        TextMeshProUGUI text1;
        TextMeshProUGUI percentage1;
        RectTransform rect1;

        TextMeshProUGUI text2;
        TextMeshProUGUI percentage2;
        RectTransform rect2;

        TextMeshProUGUI text3;
        TextMeshProUGUI percentage3;
        RectTransform rect3;

        TextMeshProUGUI text4;
        TextMeshProUGUI percentage4;
        RectTransform rect4;

        GameObject progressBar;
        GameObject currentParent;

        //voting
        Dictionary<int, int> optionLink = new Dictionary<int, int>();   //                                                option    :   index
        Dictionary<string, int> votes = new Dictionary<string, int>();  //                                                username  :   option
        Dictionary<int, int> currentVotes = new Dictionary<int, int>(); // Contains each option and how much              option    :   votes

        //config
        public ConfigEntry<string> configUsername;
        public ConfigEntry<string> configAccessToken;

        private void Awake()
        {
            // Plugin startup logic
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");

            configUsername = Config.Bind("TwitchOuath",
                                        "Username",
                                        "cryptizism",
                                        "Your twitch username!");

            configAccessToken = Config.Bind("TwitchOuath",
                                        "AccessToken",
                                        "gp762nuuoqcoxypju8c569th9wz7q5",
                                        "Your twitch Access Token! (can be found here: https://twitchtokengenerator.com/, click bot chat token, go through it and get the access token)");

            SceneManager.sceneLoaded += OnSceneLoaded;

            //Physics2D.gravity = new Vector2(0f, -10f);

            effects.Add("No Friction", fncFrictionlessEnviroment);
            //effects.Add("Send to Spawn", fncSendToSpawn);
            effects.Add("Short Hammer", fncShortHammer);
            effects.Add("Long Hammer", fncLongHammer);
            effects.Add("Spawn Object", fncSpawnObject);
            effects.Add("High Pitch", fncHighPitch);
            effects.Add("High Gravity", fncHighGravity);
            effects.Add("Low Gravity", fncLowGravity);
            effects.Add("Zoom Out", fncZoomCamera);
            effects.Add("Australian Mode", fncFlipCamera);
            effects.Add("Baba Player", fncBabyPlayer);
            effects.Add("Potato Simulator", fncLimitFPS);
            //increased effectEvery speed

            //doNotRepeat.Add(fncSendToSpawn);
            doNotRepeat.Add(fncSpawnObject);

            Bot bot = new Bot(this);
        }

        
        private void Update()
        {
            if (!isMain) return;
            /*
            if (Input.GetKeyDown("a"))
            {
                int i = effects.Count;
                System.Random rnd = new System.Random();
                for (int v = 0; v < 100; v++)
                {
                    int index = rnd.Next(0, i);
                    Console.WriteLine("Effect: " + effects.ElementAt(index).Key + " Index: " + v);
                }
            }
            */
        }
        

        public void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Logger.LogInfo("Loaded scene: " + scene.name);
            if (scene.name == "Mian")
            {
                isMain = true;
                Logger.LogInfo("We are in main!");
                player = GameObject.Find("Player");
                mountain = GameObject.Find("Mountain");
                hammer = GameObject.Find("PoleMiddle");
                woodenBarrel = GameObject.Find("SnowHat");
                cam = GameObject.FindGameObjectWithTag("MainCamera");
                narrator = GameObject.Find("Narrator");
                orange = GameObject.Find("Orange");

                assetBundle = AssetBundle.LoadFromFile(Path.Combine(Application.streamingAssetsPath, "ui"));

                var prefab = assetBundle.LoadAsset<GameObject>("INJECTED_UI_HOLDER");
                Instantiate(prefab);

                votingHolder = GameObject.Find("Voting");

                votestxt = votingHolder.transform.GetChild(0).GetComponent<TextMeshProUGUI>();

                rect1 = votingHolder.transform.GetChild(1).transform.GetChild(0).GetComponent<RectTransform>();
                text1 = votingHolder.transform.GetChild(1).transform.GetChild(1).GetComponent<TextMeshProUGUI>();
                percentage1 = votingHolder.transform.GetChild(1).transform.GetChild(2).GetComponent<TextMeshProUGUI>();

                rect2 = votingHolder.transform.GetChild(2).transform.GetChild(0).GetComponent<RectTransform>();
                text2 = votingHolder.transform.GetChild(2).transform.GetChild(1).GetComponent<TextMeshProUGUI>();
                percentage2 = votingHolder.transform.GetChild(2).transform.GetChild(2).GetComponent<TextMeshProUGUI>();

                rect3 = votingHolder.transform.GetChild(3).transform.GetChild(0).GetComponent<RectTransform>();
                text3 = votingHolder.transform.GetChild(3).transform.GetChild(1).GetComponent<TextMeshProUGUI>();
                percentage3 = votingHolder.transform.GetChild(3).transform.GetChild(2).GetComponent<TextMeshProUGUI>();

                rect4 = votingHolder.transform.GetChild(4).transform.GetChild(0).GetComponent<RectTransform>();
                text4 = votingHolder.transform.GetChild(4).transform.GetChild(1).GetComponent<TextMeshProUGUI>();
                percentage4 = votingHolder.transform.GetChild(4).transform.GetChild(2).GetComponent<TextMeshProUGUI>();

                progressBar = GameObject.Find("ProgressBar");
                currentParent = GameObject.Find("Current");

                getRandomEffects();
            }
            else
            {
                isMain = false;
                if (assetBundle)
                {
                    assetBundle.Unload(false);
                }
            }
        }

        private int occurenceInRunningList(Action<bool> action)
        {
            return runningEffects.Where(i => i != null && i == action).Count();
        }

        private bool isDisabling(bool start, Action<bool> action)
        {
            //note to self, Destroy() does not work
            if (!start)
            {
                runningEffects.Remove(action);
                Destroy(currentParent.transform.GetChild(0).gameObject);
            }

            return (!start && occurenceInRunningList(action) < 2 && !doNotRepeat.Contains(action));
        } // enabling : n & y  & n false        disabling : y & y & n false

        private void fncFrictionlessEnviroment(bool start)
        {
            PolygonCollider2D polyCollider = mountain.GetComponentInChildren<PolygonCollider2D>();
            if (isDisabling(start, fncFrictionlessEnviroment))
            {
                polyCollider.sharedMaterial.friction = 10;
            } else
            {
                polyCollider.sharedMaterial.friction = 0;
            }
        }

        /*
        private void fncSendToSpawn(bool start)
        {
            if (start)
            {
                player.transform.position = spawnLoc;
            } else
            {
                Destroy(GameObject.Find("Current").transform.GetChild(0).gameObject);
                runningEffects.Remove(fncSendToSpawn);
            }
        }
        */

        private void fncShortHammer(bool start)
        {
            if(isDisabling(start, fncShortHammer) && !runningEffects.Contains(fncLongHammer))
            {
                hammer.transform.localScale = new Vector3(1F, 1F, 1F);
            } else
            {
                hammer.transform.localScale = new Vector3(0.5F, 0.5F, 0.5F);
            }
        }

        private void fncLongHammer(bool start)
        {
            if (isDisabling(start, fncLongHammer) && !runningEffects.Contains(fncShortHammer))
            {
                hammer.transform.localScale = new Vector3(1F, 1F, 1F);
            }
            else
            {
                hammer.transform.localScale = new Vector3(1.5F, 1.5F, 1.5F);
            }
        }

        private void fncSpawnObject(bool start)
        {
            if (start)
            {
                GameObject barrel = Instantiate(woodenBarrel, new Vector3(player.transform.position.x, player.transform.position.y + 2, player.transform.position.z), Quaternion.identity);
            }
            else
            {
                Destroy(currentParent.transform.GetChild(0).gameObject);
                runningEffects.Remove(fncSpawnObject);
            }
        }

        private void fncLowGravity(bool start)
        {
            if (isDisabling(start, fncLowGravity) && !runningEffects.Contains(fncHighGravity))
            {

                Physics2D.gravity = new Vector2(0, -30);
            } else
            {
                Physics2D.gravity = new Vector2(0, -5);
            }
        }

        private void fncHighGravity(bool start)
        {
            if (isDisabling(start, fncHighGravity) && !runningEffects.Contains(fncLowGravity))
            {

                Physics2D.gravity = new Vector2(0, -30);
            } else
            {
                Physics2D.gravity = new Vector2(0, -55);
            }
            Physics2D.gravity = Physics2D.gravity.y == -30 ? new Vector2(0, -55) : new Vector2(0, -30);
        }

        private void fncZoomCamera(bool start)
        {
            Camera camera = cam.GetComponent<Camera>();
            if (isDisabling(start, fncZoomCamera))
            {
                camera.orthographicSize = 5;
            }
            else
            {
                camera.orthographicSize = 20;
            }
        }

        private void fncHighPitch(bool start)
        {
            AudioSource narratorSource = narrator.GetComponent<AudioSource>();
            if (isDisabling(start, fncHighPitch))
            {
                narratorSource.pitch = 1;
            } else
            {
                narratorSource.pitch = 2;
            }
        }

        /*
        private void fncLowPitch()
        {
            AudioSource narratorSource = narrator.GetComponent<AudioSource>();
            narratorSource.pitch = narratorSource.pitch == 1 ? -1 : 1;
        }
        */

        private void fncFlipCamera(bool start)
        {
            if (isDisabling(start, fncFlipCamera))
            {
                cam.transform.rotation = Quaternion.Euler(0, 0, 0);
            } else
            {
                cam.transform.rotation = Quaternion.Euler(0, 0, -180);
            }
        }

        private void fncBabyPlayer(bool start)
        {
            if (isDisabling(start, fncBabyPlayer))
            {
                player.transform.localScale = new Vector3(1F, 1F, 1F);
            }
            else
            {
                player.transform.localScale = new Vector3(0.5F, 0.5F, 0.5F);
            }
        }

        /*
        private void fncExplodingOrange(bool start)
        {
            GameObject gameObject = Instantiate(orange, new Vector3(player.transform.position.x, player.transform.position.y + 2, player.transform.position.z), Quaternion.identity);
            helperExplodingOrage(3, gameObject);
        }

        IEnumerator helperExplodingOrage(int seconds, GameObject gameObject)
        {
            yield return new WaitForSeconds(seconds);
            Collider[] colided = Physics.OverlapSphere(gameObject.transform.position, 5f);
            foreach(Collider nearObject in colided)
            {
                Rigidbody2D rb = nearObject.GetComponent<Rigidbody2D>();
                if (rb == null) continue;
                Vector2 distanceVector = nearObject.transform.position - gameObject.transform.position;
                rb.AddForce(distanceVector.normalized * 100);
            }
        }
        */

        
        private void fncLimitFPS(bool start)
        {
            if (isDisabling(start, fncLimitFPS))
            {
                QualitySettings.vSyncCount = PlayerPrefs.GetInt("Vsync");
                Application.targetFrameRate = PlayerPrefs.GetInt("ResolutionRefresh");
            } else
            {
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = 10;
            }
        }
        

        public void getRandomEffects()
        {
            offset = offset == 0 ? 4 : 0;

            optionLink.Clear();

            int[] effectIndexes = new int[4];

            int i = effects.Count;
            System.Random rnd = new System.Random();

            for (int v = 0; v < 3;)
            {
                int index = rnd.Next(0, i);
                bool isUsed = false;
                foreach (int ind in effectIndexes)
                {
                    if(index == ind)
                    {
                        isUsed = true;
                    }
                }
                if (isUsed) continue;
                effectIndexes[v] = index;
                optionLink[v + 1 + offset] = index;
                v++;
            }

            votes.Clear();
            currentVotes.Clear();

            votestxt.text = "Total Votes: 0";
            progressBar.transform.localScale = new Vector2(0, progressBar.transform.localScale.y);

            currentVotes.Add(1 + offset, 0);
            text1.text = $"{1+offset}. {effects.ElementAt(effectIndexes[0]).Key}";
            percentage1.text = "0%";
            rect1.localScale = new Vector2(0, rect1.localScale.y);

            currentVotes.Add(2 + offset, 0);
            text2.text = $"{2 + offset}. {effects.ElementAt(effectIndexes[1]).Key}";
            percentage2.text = "0%";
            rect2.localScale = new Vector2(0, rect2.localScale.y);

            currentVotes.Add(3 + offset, 0);
            text3.text = $"{3 + offset}. {effects.ElementAt(effectIndexes[2]).Key}";
            percentage3.text = "0%";
            rect3.localScale = new Vector2(0, rect3.localScale.y);

            currentVotes.Add(4 + offset, 0);
            text4.text = $"{4 + offset}. Random Effect";
            percentage4.text = "0%";
            rect4.localScale = new Vector2(0, rect4.localScale.y);

            for (int v = 0; v < 1;)
            {
                int index = rnd.Next(i);
                bool isUsed = false;
                foreach (int ind in effectIndexes)
                {
                    if (index == ind)
                    {
                        isUsed = true;
                    }
                }
                if (isUsed) continue;
                effectIndexes[3] = index;
                optionLink[4 + offset] = index;
                v++;
            }

            LeanTween.scaleX(progressBar, 1, effectEvery).setOnComplete(applyEffect);
        }

        private void applyEffect()
        {
            var maxVotes = currentVotes.OrderByDescending(x => x.Value).FirstOrDefault();
            int index = optionLink[maxVotes.Key];
            effects.ElementAt(index).Value(true);
            var prefab = assetBundle.LoadAsset<GameObject>("EffectHolder");
            var instPrefab = Instantiate(prefab);
            instPrefab.transform.SetParent(currentParent.transform);
            instPrefab.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = effects.ElementAt(index).Key;
            var progressBarEffect = instPrefab.transform.GetChild(1).transform.Find("ProgressBar").gameObject;
            getRandomEffects();
            endEffect(effectLast, effects.ElementAt(index).Value, progressBarEffect);
        }

        private void endEffect(float time, Action<bool> effect, GameObject gameObject)
        {
            if (doNotRepeat.Contains(effect))
            {
                gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 0);
                gameObject.transform.parent.GetComponent<UnityEngine.UI.Image>().color = new Color(0, 0, 0, 0);
            }
            LeanTween.scaleX(gameObject, 1, time).setOnComplete(() => { effect(false); });
        }

        public void voteCast(int vote, string username)
        {
            if (!currentVotes.ContainsKey(vote)) return;
            if (votes.ContainsKey(username))
            {
                if (votes[username] == vote) return;
                int oldVote = votes[username];
                currentVotes[oldVote] = currentVotes[oldVote] - 1;
                votes[username] = vote;
            } else
            {
                votes.Add(username, vote);
            }
            currentVotes[vote] = currentVotes[vote] + 1;

            int totalVotes = 0;

            foreach(KeyValuePair<int, int> entry in currentVotes)
            {
                totalVotes += entry.Value;
            }


            percentage1.text = $"{Decimal.Round((currentVotes[1 + offset] / totalVotes)*100)}%";
            rect1.localScale = new Vector2(currentVotes[1 + offset] / totalVotes, rect1.localScale.y);

            percentage2.text = $"{Decimal.Round((currentVotes[2 + offset] / totalVotes) * 100)}%";
            rect2.localScale = new Vector2(currentVotes[2 + offset] / totalVotes, rect2.localScale.y);

            percentage3.text = $"{Decimal.Round((currentVotes[3 + offset] / totalVotes) * 100)}%";
            rect3.localScale = new Vector2(currentVotes[3 + offset] / totalVotes, rect3.localScale.y);

            percentage4.text = $"{Decimal.Round((currentVotes[4 + offset] / totalVotes) * 100)}%";
            rect4.localScale = new Vector2(currentVotes[4 + offset] / totalVotes, rect4.localScale.y);

            votestxt.text = $"Total Votes: {totalVotes}";
        }
    }
}

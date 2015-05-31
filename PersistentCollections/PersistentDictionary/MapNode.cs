using PersistentCollections.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PersistentCollections.PersistentDictionary
{
    internal class MapNode<K, V> : IMapNode<K, V>, IEnumerable<KeyValuePair<K, V>>, IEquatable<MapNode<K, V>>
    {
        private KeyValuePair<K, V>[] values;
        private ICollisionCollection<K, V>[] collisions;
        private IMapNode<K, V>[] references;
        private int hash;

        private UInt32 vBitmap, rBitmap, cBitmap;
        private VersionID versionID;

      
        private MapNode() { }

        public MapNode(int idx, K key, V value, VersionID versionID)
        {
            values = new[] { new KeyValuePair<K, V>(key, value) };
            vBitmap = (UInt32)(1 << idx);

            this.versionID = versionID;
        }

        public int ValueCount
        {
            get
            {
                var count = 0;
                if (values != null) count += values.Length;
                if (collisions != null) count += collisions.Length;

                return count;
            }
        }

        public int ReferenceCount
        {
            get
            {
                return (references != null)
                    ? references.Length
                    : 0;
            }
        }

        #region API functions

        private int ValuePosition(int i) { return (i == 31) ? 0 : (vBitmap >> i + 1).BitCount(); }
        private int CollisionPosition(int i) { return (i == 31) ? 0 : (cBitmap >> i + 1).BitCount(); }
        private int ReferencePosition(int i) { return (i == 31) ? 0 : (rBitmap >> i + 1).BitCount(); }

        public NodeState GetNodeStateAt(int i)
        {
            var pos = (1 << i);

            if ((rBitmap & pos) != 0) return NodeState.Reference;
            if ((vBitmap & pos) != 0) return NodeState.Value;
            if ((cBitmap & pos) != 0) return NodeState.Collision;

            return NodeState.Nil;
        }

        private ICollisionCollection<K, V> CreateCollisionCollection(VersionID versionID, params KeyValuePair<K, V>[] pairs)
        {
            return new CollisionArray<K, V>(versionID, pairs);
        }

        public V GetValueAt(int i, NodeState state, K key)
        {
            var pair = (state == NodeState.Value)
                ? values[ValuePosition(i)]
                : collisions[CollisionPosition(i)].GetItem(key);

            if (!key.Equals(pair.Key))
                throw new KeyNotFoundException("The persistent dictionary doesn't contain value associated with specified key");

            return pair.Value;
        }

        public bool IsKeyAt(int idx, NodeState state, K key)
        {
            return (state == NodeState.Value)
                ? values[ValuePosition(idx)].Key.Equals(key)
                : collisions[CollisionPosition(idx)].HasItemWithKey(key);
        }

        public IMapNode<K, V> GetReferenceAt(int i)
        {
            return references[ReferencePosition(i)];
        }

        private KeyValuePair<K, V>[] AddToValues(int vIndex, K key, V value)
        {
            if (values == null) return new[] { new KeyValuePair<K, V>(key, value) };

            var newValues = new KeyValuePair<K, V>[values.Length + 1];

            Array.Copy(values, newValues, vIndex);
            newValues[vIndex] = new KeyValuePair<K, V>(key, value);
            Array.Copy(values, vIndex, newValues, vIndex + 1, values.Length - vIndex);

            return newValues;
        }

        private ICollisionCollection<K, V>[] AddToCollisions(int cIndex, ICollisionCollection<K, V> collision)
        {
            if (collisions == null) return new[] { collision };

            var newCollisions = new ICollisionCollection<K, V>[collisions.Length + 1];

            Array.Copy(collisions, newCollisions, cIndex);
            newCollisions[cIndex] = collision;
            Array.Copy(collisions, cIndex, newCollisions, cIndex + 1, collisions.Length - cIndex);

            return newCollisions;
        }

        public IMapNode<K, V> AddValueItemAt(int i, K key, V value, VersionID versionID = null)
        {
            var newCollisions = collisions;
            var newReferences = references;

            if (versionID != null && versionID != this.versionID)
            {
                newCollisions = collisions.Copy();
                newReferences = references.Copy();
            }

            var newValues = AddToValues(ValuePosition(i), key, value);

            return new MapNode<K, V>()
            {
                values = newValues,
                collisions = newCollisions,
                references = newReferences,
                vBitmap = vBitmap | (1u << i),
                cBitmap = cBitmap,
                rBitmap = rBitmap,
                versionID = versionID
            };
        }


        public MapNodeRelation RelationWithNodeAt(K key, int idx, NodeState state)
        {
            if (state == NodeState.Value)
            {
                var pair = values[ValuePosition(idx)];

                if (pair.Key.GetHashCode() == key.GetHashCode())
                {
                    return (pair.Key.Equals(key))
                        ? MapNodeRelation.Equal
                        : MapNodeRelation.Collide;
                }

                return MapNodeRelation.Different;
            }

            else //if (state == NodeState.Collision)
            {
                var cCollection = collisions[CollisionPosition(idx)];
                if (cCollection.HashCode == key.GetHashCode())
                {
                    return (cCollection.HasItemWithKey(key))
                        ? MapNodeRelation.Equal
                        : MapNodeRelation.Collide;
                }

                return MapNodeRelation.Different;
            }
        }

        public IMapNode<K, V> AddToColisionAt(int i, K key, V value, VersionID versionID)
        {
            var cIndex = CollisionPosition(i);

            var newCollisions = collisions;
            var newValues = values;
            var newReferences = references;

            if (versionID == null || this.versionID != versionID)
            {
                newCollisions = collisions.Copy();
            }

            newCollisions[cIndex] = newCollisions[cIndex].Add(key, value, versionID);

            if (versionID != null && versionID != this.versionID)
            {
                newReferences = references.Copy();
                newValues = values.Copy();
            }

            return new MapNode<K, V>()
            {
                values = newValues,
                collisions = newCollisions,
                references = newReferences,
                vBitmap = vBitmap,
                cBitmap = cBitmap,
                rBitmap = rBitmap,
                versionID = versionID
            };
        }

        public IMapNode<K, V> CreateCollisionAt(int i, K key, V value, VersionID versionID)
        {
            var pos = (1u << i);

            var cIndex = CollisionPosition(i);
            var vIndex = ValuePosition(i);

            var newValues = RemoveFromValues(vIndex);
            ICollisionCollection<K, V>[] newCollisions;

            if (collisions != null)
            {
                newCollisions = new ICollisionCollection<K, V>[collisions.Length + 1];
                Array.Copy(collisions, newCollisions, cIndex);
                Array.Copy(collisions, cIndex, newCollisions, cIndex + 1, collisions.Length - cIndex);
            }
            else
            {
                newCollisions = new ICollisionCollection<K, V>[1]; ;
            }

            newCollisions[cIndex] = CreateCollisionCollection(
                    versionID,
                    values[vIndex],
                    new KeyValuePair<K, V>(key, value)
                );

            var newReferences = (versionID != null && versionID != this.versionID)
                ? references.Copy()
                : references;

            return new MapNode<K, V>()
            {
                values = newValues,
                collisions = newCollisions,
                references = newReferences,
                vBitmap = vBitmap & ~pos,
                cBitmap = cBitmap | pos,
                rBitmap = rBitmap,
                versionID = versionID
            };
        }

        private KeyValuePair<K, V>[] RemoveFromValues(int vIndex)
        {
            if (values.Length == 1) return null;

            var newValues = new KeyValuePair<K, V>[values.Length - 1];

            Array.Copy(values, newValues, vIndex);
            Array.Copy(values, vIndex + 1, newValues, vIndex, values.Length - vIndex - 1);

            return newValues;
        }

        private ICollisionCollection<K, V>[] RemoveFromCollisions(int cIndex)
        {
            if (collisions.Length == 1) return null;

            var newCollisions = new ICollisionCollection<K, V>[collisions.Length - 1];

            Array.Copy(collisions, newCollisions, cIndex);
            Array.Copy(collisions, cIndex + 1, newCollisions, cIndex, collisions.Length - cIndex - 1);

            return newCollisions;
        }

        private MapNode<K, V>[] RemoveFromReferences(int rIndex)
        {
            if (references.Length == 1) return null;

            var newReferences = new MapNode<K, V>[references.Length - 1];

            Array.Copy(references, newReferences, rIndex);
            Array.Copy(references, rIndex + 1, newReferences, rIndex, references.Length - rIndex - 1);

            return newReferences;
        }

        public IMapNode<K, V> CreateReference(int i, IMapNode<K, V> mapNode, NodeState state, VersionID versionID)
        {
            var pos = 1u << i;

            var rIndex = ReferencePosition(i);
            IMapNode<K, V>[] newReferences;

            if (references == null)
            {
                newReferences = new[] { mapNode };
            }
            else
            {
                newReferences = new MapNode<K, V>[references.Length + 1];
                Array.Copy(references, newReferences, rIndex);
                newReferences[rIndex] = mapNode;
                Array.Copy(references, rIndex, newReferences, rIndex + 1, references.Length - rIndex);
            }

            var newCBitmap = cBitmap;
            var newVBitmap = vBitmap;

            var newValues = values;
            var newColisions = collisions;

            if (state == NodeState.Value)
            {
                newVBitmap &= ~pos;
                newValues = RemoveFromValues(ValuePosition(i));
            }
            else
            {
                newCBitmap &= ~pos;
                newColisions = RemoveFromCollisions(CollisionPosition(i));
            }

            if (versionID != null)
            {
                if (this.versionID == versionID)
                {
                    values = newValues;
                    collisions = newColisions;
                    references = newReferences;
                    cBitmap = newCBitmap;
                    vBitmap = newVBitmap;
                    rBitmap = rBitmap | pos;

                    return this;
                }

                if (newColisions == collisions) newColisions = collisions.Copy();
                if (newValues == values) newValues = values.Copy();
            }

            return new MapNode<K, V>()
            {
                values = newValues,
                collisions = newColisions,
                references = newReferences,
                cBitmap = newCBitmap,
                vBitmap = newVBitmap,
                rBitmap = rBitmap | pos,
                versionID = versionID
            };
        }

        public IMapNode<K, V> ChangeReference(int i, IMapNode<K, V> mapNode, VersionID versionID)
        {
            var newValues = values;
            var newCollisions = collisions;

            var rIndex = ReferencePosition(i);

            if (versionID != null)
            {
                if (this.versionID == versionID)
                {
                    references[rIndex] = mapNode;
                    return this;
                }

                newValues = values.Copy();
                newCollisions = collisions.Copy();
            }

            var newReferences = references.Copy();
            newReferences[rIndex] = mapNode;

            return new MapNode<K, V>()
            {
                values = newValues,
                collisions = newCollisions,
                references = newReferences,
                vBitmap = vBitmap,
                cBitmap = cBitmap,
                rBitmap = rBitmap,
                versionID = versionID
            };
        }

        private ICollisionCollection<K, V>[] ChangeCollisions(int cIndex, K key, V value, VersionID versionID)
        {
            if (versionID != null && versionID == this.versionID)
            {
                collisions[cIndex] = collisions[cIndex].Change(key, value, versionID);
                return collisions;
            }

            var newCollisions = collisions.Copy();
            var collision = newCollisions[cIndex];

            newCollisions[cIndex] = collision.Change(key, value, versionID);

            return newCollisions;
        }

        private KeyValuePair<K, V>[] ChangeValues(int vIndex, K key, V value, VersionID versionID)
        {
            var newPair = new KeyValuePair<K, V>(key, value);

            if (versionID != null && versionID == this.versionID)
            {
                values[vIndex] = newPair;
                return values;
            }

            var newValues = values.Copy();
            newValues[vIndex] = newPair;

            return newValues;
        }

        public IMapNode<K, V> ChangeValue(int idx, NodeState state, K key, V value, VersionID versionID)
        {
            var newValues = values;
            var newCollisions = collisions;
            var newReferences = references;

            if (state == NodeState.Value)
            {
                newValues = ChangeValues(ValuePosition(idx), key, value, versionID);
            }
            if (state == NodeState.Collision)
            {
                newCollisions = ChangeCollisions(CollisionPosition(idx), key, value, versionID);
            }

            if (versionID != null && this.versionID != versionID)
            {
                newReferences = references.Copy();
                if (values == newValues) newValues = values.Copy();
                if (collisions == newCollisions) newCollisions = collisions.Copy();
            }

            return new MapNode<K, V>()
            {
                values = newValues,
                collisions = newCollisions,
                references = newReferences,
                vBitmap = vBitmap,
                cBitmap = cBitmap,
                rBitmap = rBitmap,
                versionID = versionID
            };
        }

        public IMapNode<K, V> CreateNewNodeFrom(int oldIdx, K key, V value, int idx1, int idx2, VersionID versionID)
        {
            var otherValue = new KeyValuePair<K, V>(key, value);

            if ((vBitmap & (1u << oldIdx)) != 0)
            {
                // On index oldIdx is value
                var vIndex = ValuePosition(oldIdx);
                var thisValue = values[vIndex];

                return new MapNode<K, V>()
                {
                    vBitmap = (1u << idx1) | (1u << idx2),
                    values = (idx1 < idx2)
                        ? new[] { otherValue, thisValue }
                        : new[] { thisValue, otherValue },
                    versionID = versionID
                };
            }
            else
            {
                // On index oldIdx is collision collection
                var cIndex = CollisionPosition(oldIdx);
                var thisValue = collisions[cIndex];

                return new MapNode<K, V>()
                {
                    cBitmap = (1u << idx1),
                    vBitmap = (1u << idx2),
                    values = new[] { otherValue },
                    collisions = new[] { thisValue },
                    versionID = versionID
                };
            }
        }

        public int GetHashCodeAt(int idx, NodeState state)
        {
            return (state == NodeState.Value)
                ? values[ValuePosition(idx)].Key.GetHashCode()
                : collisions[CollisionPosition(idx)].HashCode;
        }

        public IMapNode<K, V> CreateReferenceNode(int idx, IMapNode<K, V> node, VersionID versionID)
        {
            return new MapNode<K, V>()
            {
                rBitmap = 1u << idx,
                references = new[] { node },
                versionID = versionID
            };
        }

        public IMapNode<K, V> RemoveValue(int idx, NodeState state, K key, VersionID versionID)
        {
            var pos = 1u << idx;

            var newCollisions = collisions;
            var newValues = values;
            var newReferences = references;
            var newCBitmap = cBitmap;
            var newVBitmap = vBitmap;

            if (state == NodeState.Collision)
            {
                var cIndex = CollisionPosition(idx);

                var collision = collisions[cIndex];
                if (collision.Count == 2)
                {
                    var vIndex = ValuePosition(idx);

                    newCollisions = RemoveFromCollisions(cIndex);
                    var pair = collision.GetRemainingValue(removedKey: key);

                    newValues = AddToValues(vIndex, pair.Key, pair.Value);
                    newCBitmap &= ~pos;
                    newVBitmap |= pos;
                }
                else
                {
                    if (versionID == null || versionID != this.versionID)
                    {
                        newCollisions = collisions.Copy();
                    }
                    newCollisions[cIndex] = collision.Remove(key, versionID);
                }
            }
            else // if (state == NodeState.Value)
            {
                newValues = RemoveFromValues(ValuePosition(idx));
                newVBitmap &= ~pos;
            }

            if (versionID != null)
            {
                if (this.versionID == versionID)
                {
                    vBitmap = newVBitmap;
                    cBitmap = newCBitmap;

                    values = newValues;
                    collisions = newCollisions;

                    return this;
                }

                if (newValues == values) newValues = values.Copy();
                if (newCollisions == collisions) newCollisions = collisions.Copy();
                newReferences = references.Copy();
            }

            return new MapNode<K, V>()
            {
                vBitmap = newVBitmap,
                cBitmap = newCBitmap,
                rBitmap = rBitmap,
                values = newValues,
                collisions = newCollisions,
                references = newReferences,
                versionID = versionID
            };
        }

        public IMapNode<K, V> Merge(IMapNode<K, V> newNode, int index, VersionID versionID)
        {
            var pos = 1u << index;

            var mapNode = newNode as MapNode<K, V>;
            if (mapNode == null) throw new NotSupportedException();

            var newCollisions = collisions;
            var newValues = values;
            var newReferences = RemoveFromReferences(ReferencePosition(index));

            var newCBitmap = cBitmap;
            var newVBitmap = vBitmap;
            var newRBitmap = rBitmap & ~pos;

            if (mapNode.values != null)
            {
                var valuePair = mapNode.values[0];
                newValues = AddToValues(ValuePosition(index), valuePair.Key, valuePair.Value);
                newVBitmap |= pos;
            }
            else // if (mapNode.collisions != null)
            {
                var collision = mapNode.collisions[0];
                newCollisions = AddToCollisions(CollisionPosition(index), collision);
                newCBitmap |= pos;
            }

            if (versionID != null)
            {
                if (this.versionID == versionID)
                {
                    vBitmap = newVBitmap;
                    cBitmap = newCBitmap;
                    rBitmap = newRBitmap;

                    values = newValues;
                    collisions = newCollisions;
                    references = newReferences;
                    return this;
                }

                if (newValues == values) newValues = values.Copy();
                if (newCollisions == collisions) newCollisions = collisions.Copy();
            }

            return new MapNode<K, V>()
            {
                rBitmap = newRBitmap,
                vBitmap = newVBitmap,
                cBitmap = newCBitmap,
                values = newValues,
                collisions = newCollisions,
                references = newReferences,
                versionID = versionID
            };
        }

        public IMapNode<K, V> MakeRoot(VersionID versionID = null)
        {
            var newCBitmap = cBitmap;
            var newVBitmap = vBitmap;

            if (values != null)
            {
                var idx = values[0].Key.GetHashCode() & 0x01f;
                newVBitmap = 1u << idx;
            }
            else
            {
                var idx = collisions[0].HashCode & 0x01f;
                newCBitmap = 1u << idx;
            }

            var newValues = values;
            var newCollisions = collisions;

            if (versionID != null)
            {
                if (this.versionID == versionID)
                {
                    vBitmap = newVBitmap;
                    cBitmap = newCBitmap;
                    return this;
                }
                else
                {
                    newValues = values.Copy();
                    newCollisions = collisions.Copy();
                }
            }

            return new MapNode<K, V>()
            {
                vBitmap = newVBitmap,
                cBitmap = newCBitmap,
                values = newValues,
                collisions = newCollisions,
                versionID = versionID
            };
        }
        #endregion
        
        public IEnumerator<KeyValuePair<K, V>> GetEnumerator()
        {
            var res = Enumerable.Empty<KeyValuePair<K, V>>();

            if (values != null)
                res = res.Concat(values);

            if (collisions != null)
                res = res.Concat(collisions.SelectMany(x => x));

            if (references != null)
            {
                res = res.Concat(references.SelectMany(x => x));
            }

            return res.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public override int GetHashCode()
        {
            if (hash == 0)
            {
                if (values != null)
                {
                    foreach (var value in values)
                        hash ^= value.Key.GetHashCode() ^ value.Value.GetHashCode();
                }
                if (collisions != null)
                {
                    foreach (var value in collisions.SelectMany(x => x)) 
                        hash ^= value.Key.GetHashCode() ^ value.Value.GetHashCode();
                }
                if (references != null)
                {
                    foreach (var node in references) hash ^= node.GetHashCode();
                }

                if (hash == 0) hash = 1;
            }

            return hash;
        }

        public override bool Equals(object obj)
        {
            var mapNode = obj as MapNode<K, V>;

            return mapNode != null && Equals(mapNode);
        }

        public bool Equals(MapNode<K, V> other)
        {
            // Compare values
            if ((values != null) == (other.values != null))
            {
                if (values != null && !object.ReferenceEquals(values, other.values))
                {
                    if (values.Length != other.values.Length) return false;
                    for (int i = 0; i < values.Length; i++)
                        if (!values[i].Equals(other.values[i])) return false;
                }
            }
            else return false;

            // Compare collision collections
            if ((collisions != null) == (other.collisions != null))
            {
                if (collisions != null && !object.ReferenceEquals(collisions, other.collisions))
                {
                    if (collisions.Length != other.collisions.Length) return false;
                    for (int i = 0; i < collisions.Length; i++)
                    {
                        var c1 = collisions[i];
                        var c2 = other.collisions[i];
                        if (!object.ReferenceEquals(c1, c2) && !c1.ContentEqual(c2)) return false;
                    }
                }
            }
            else return false;

            // Compare references
            if ((references != null) == (other.references != null))
            {
                if (references != null)
                {
                    if (references.Length != other.references.Length) return false;
                    for (int i = 0; i < references.Length; i++)
                    {
                        var r1 = references[i];
                        var r2 = other.references[i];

                        if (!object.ReferenceEquals(r1, r2) && !r1.Equals(r2)) return false;
                    }
                }
            }

            return true;
        }
    }
}

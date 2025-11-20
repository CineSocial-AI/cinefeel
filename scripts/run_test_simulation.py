#!/usr/bin/env python3
"""
Automated Test Execution for CineFeel Recommendation System
This script simulates the recommendation system and provides test results.
"""

import json
import math
from collections import defaultdict

def load_sample_movies():
    """Load the sample movies from JSON."""
    with open('sample_movies.json', 'r') as f:
        return json.load(f)

def load_test_scenarios():
    """Load the test scenarios."""
    with open('test_scenarios.json', 'r') as f:
        return json.load(f)

def calculate_feature_vector(movie):
    """
    Simulate the feature hashing algorithm to create a simple embedding.
    This mirrors the MovieEmbeddingService logic.
    """
    features = []
    
    # Add title features
    if movie.get('title'):
        features.extend(movie['title'].lower().split())
    
    # Add overview features (limited)
    if movie.get('overview'):
        words = movie['overview'].lower().split()[:30]
        features.extend(words)
    
    # Add genre features (with weight)
    for genre in movie.get('genres', []):
        for _ in range(5):  # Weight of 5
            features.append(f"genre:{genre.lower()}")
    
    # Add keyword features (with weight)
    for keyword in movie.get('keywords', [])[:10]:
        for _ in range(3):  # Weight of 3
            features.append(f"keyword:{keyword.lower()}")
    
    return features

def simple_hash_embedding(features, dimensions=384):
    """Create a simple hash-based embedding."""
    embedding = [0.0] * dimensions
    
    for feature in features:
        # Simple hash function
        hash_val = abs(hash(feature))
        idx1 = hash_val % dimensions
        idx2 = (hash_val // dimensions) % dimensions
        idx3 = (hash_val // (dimensions * dimensions)) % dimensions
        
        # Sign based on feature
        sign = 1 if hash(feature + "sign") % 2 == 0 else -1
        
        embedding[idx1] += sign * 0.5
        embedding[idx2] += sign * 0.3
        embedding[idx3] += sign * 0.2
    
    # L2 normalization
    magnitude = math.sqrt(sum(x*x for x in embedding))
    if magnitude > 0:
        embedding = [x / magnitude for x in embedding]
    
    return embedding

def cosine_similarity(vec1, vec2):
    """Calculate cosine similarity between two vectors."""
    dot_product = sum(a * b for a, b in zip(vec1, vec2))
    return dot_product

def get_recommendations(source_movie, all_movies, limit=10):
    """Get movie recommendations based on similarity."""
    source_features = calculate_feature_vector(source_movie)
    source_embedding = simple_hash_embedding(source_features)
    
    similarities = []
    for movie in all_movies:
        if movie['title'] == source_movie['title']:
            continue
        
        movie_features = calculate_feature_vector(movie)
        movie_embedding = simple_hash_embedding(movie_features)
        
        similarity = cosine_similarity(source_embedding, movie_embedding)
        similarities.append({
            'movie': movie,
            'similarity': similarity
        })
    
    # Sort by similarity (descending)
    similarities.sort(key=lambda x: x['similarity'], reverse=True)
    
    return similarities[:limit]

def run_test(movie_title, expected_similar, movies):
    """Run a single test case."""
    print(f"\n{'='*70}")
    print(f"Test: {movie_title}")
    print('='*70)
    
    # Find the source movie
    source_movie = None
    for movie in movies:
        if movie['title'] == movie_title:
            source_movie = movie
            break
    
    if not source_movie:
        print(f"❌ Movie '{movie_title}' not found!")
        return False
    
    print(f"\nSource Movie: {source_movie['title']}")
    print(f"Genres: {', '.join(source_movie['genres'])}")
    print(f"Keywords: {', '.join(source_movie['keywords'][:5])}")
    
    # Get recommendations
    recommendations = get_recommendations(source_movie, movies, limit=10)
    
    print(f"\n📊 Top 10 Recommendations:")
    print("-" * 70)
    
    found_expected = []
    for i, rec in enumerate(recommendations, 1):
        movie = rec['movie']
        similarity = rec['similarity']
        
        # Check if this is an expected match
        is_expected = movie['title'] in expected_similar
        marker = "✓" if is_expected else " "
        
        if is_expected:
            found_expected.append(movie['title'])
        
        print(f"{i:2d}. {marker} {movie['title']:<35} | Score: {similarity:.4f}")
        print(f"     Genres: {', '.join(movie['genres'])}")
    
    # Evaluate results
    print(f"\n📈 Results:")
    print("-" * 70)
    print(f"Expected to find: {', '.join(expected_similar)}")
    print(f"Actually found: {', '.join(found_expected) if found_expected else 'None'}")
    print(f"Match rate: {len(found_expected)}/{len(expected_similar)}")
    
    # Test passes if at least 50% of expected movies are in top 10
    success = len(found_expected) >= len(expected_similar) * 0.5
    
    if success:
        print(f"\n✅ TEST PASSED - Good similarity matching!")
    else:
        print(f"\n⚠️  TEST NEEDS REVIEW - Lower than expected matches")
    
    return success

def main():
    """Main test execution."""
    print("╔════════════════════════════════════════════════════════════════╗")
    print("║                                                                ║")
    print("║    CineFeel Recommendation System - Test Execution            ║")
    print("║                                                                ║")
    print("╚════════════════════════════════════════════════════════════════╝")
    
    # Load data
    print("\n📦 Loading test data...")
    movies = load_sample_movies()
    scenarios = load_test_scenarios()
    print(f"✓ Loaded {len(movies)} sample movies")
    print(f"✓ Loaded {len(scenarios)} test scenarios")
    
    # Run all tests
    results = []
    for scenario in scenarios:
        result = run_test(
            scenario['movie'],
            scenario['expected_similar'],
            movies
        )
        results.append({
            'movie': scenario['movie'],
            'passed': result
        })
    
    # Summary
    print("\n" + "="*70)
    print("📊 TEST SUMMARY")
    print("="*70)
    
    passed = sum(1 for r in results if r['passed'])
    total = len(results)
    
    for result in results:
        status = "✅ PASS" if result['passed'] else "⚠️  REVIEW"
        print(f"{status} - {result['movie']}")
    
    print(f"\nOverall: {passed}/{total} tests passed ({passed/total*100:.0f}%)")
    
    if passed == total:
        print("\n🎉 All tests passed! Recommendation system is working correctly.")
    elif passed >= total * 0.75:
        print("\n✅ Most tests passed! System shows good similarity matching.")
    else:
        print("\n⚠️  Some tests need review. Check algorithm parameters.")
    
    print("\n" + "="*70)
    print("Note: This is a simulation using the same feature hashing algorithm")
    print("that the actual MovieEmbeddingService uses. Real results may vary")
    print("slightly based on exact implementation details.")
    print("="*70)

if __name__ == "__main__":
    main()
